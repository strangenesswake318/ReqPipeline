using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ReqPipeline.Core.Interfaces;
using ReqPipeline.Core.Models;
using ReqPipeline.Core.Utils;

namespace ReqPipeline.Core.StaticAnalysis;

// 💡 UnifiedReviewPromptが出力する純粋な配列要素を受け取るためのクラス
public class SemanticIssue
{
    [JsonPropertyName("NodeId")] public string NodeId { get; set; } = "";
    [JsonPropertyName("RuleId")] public string RuleId { get; set; } = "";
    [JsonPropertyName("Message")] public string Message { get; set; } = "";
    [JsonPropertyName("Severity")] public string Severity { get; set; } = "Warning";
}

public class SemanticValidator : IRequirementStaticAnalysis
{
    private readonly ILlmClient _llmClient;
    private readonly IKnowledgeBase _knowledgeBase;

    // AI呼び出しに必要なクライアントと、ナレッジ取得用の部品をもらう
    public SemanticValidator(ILlmClient llmClient, IKnowledgeBase knowledgeBase)
    {
        _llmClient = llmClient;
        _knowledgeBase = knowledgeBase;
    }

    public async Task ValidateAsync(PipelineContext context)
    {
        if (context.HasFatalError()) return;

        // 1. 各種テキスト（材料）の構築
        var treeText = BuildTreeText(context.Nodes);

        // 用語集の抽出
        var usedNamespaces = ExtractUsedNamespaces(context.Nodes);
        var relevantGlossary = context.Glossary.Entries
            .Where(e => usedNamespaces.Contains(e.Namespace) || e.Namespace == "SYS")
            .ToList();

        var glossaryMd = new StringBuilder();
        if (relevantGlossary.Any())
        {
            glossaryMd.AppendLine("【ドメイン知識（用語集）】");
            foreach (var entry in relevantGlossary)
            {
                glossaryMd.AppendLine($"- **{entry.FullName}** ({entry.Category}): {entry.Definition}");
            }
        }

        // ナレッジ（山本メソッド）の取得
        var knowledge = await _knowledgeBase.GetRelevantKnowledgeAsync(context);

        // 2. 統合プロンプトファイルの読み込みと置換
        var promptPath = ResourceLocator.FindFile("UnifiedReviewPrompt.md");
        if (!File.Exists(promptPath))
        {
            throw new FileNotFoundException($"プロンプトファイルが見つかりません: UnifiedReviewPrompt.md");
        }
        var promptTemplate = await File.ReadAllTextAsync(promptPath);

        var prompt = promptTemplate
            .Replace("{{KnowledgeBase}}", knowledge)
            .Replace("{{glossaryMd}}", glossaryMd.ToString())
            .Replace("{{Requirements}}", treeText);

        // ==========================================
        // 💡 復活！コンソールログ：AIに送るプロンプトを全表示
        // ==========================================
        Console.WriteLine("\n--------------------------------------");
        Console.WriteLine("🚀 【SemanticValidator: AI送信プロンプト】");
        Console.WriteLine(prompt);
        Console.WriteLine("--------------------------------------\n");

        try
        {
            // 3. LLM呼び出し
            var responseJson = await _llmClient.GenerateTextAsync(prompt, 0.0f);

            // ==========================================
            // 💡 復活！コンソールログ：AIからの生回答を表示
            // ==========================================
            Console.WriteLine("\n--------------------------------------");
            Console.WriteLine("🤖 【SemanticValidator: AIからの生回答 (JSON)】");
            Console.WriteLine(responseJson);
            Console.WriteLine("--------------------------------------\n");

            if (string.IsNullOrWhiteSpace(responseJson)) return;

            // 4. JSONパースとIssue登録
            var cleanJson = ExtractJson(responseJson);
            var issues = JsonSerializer.Deserialize<List<SemanticIssue>>(cleanJson);

            if (issues != null)
            {
                foreach (var issue in issues)
                {
                    var severity = issue.Severity.Equals("Error", StringComparison.OrdinalIgnoreCase) ? Severity.Error : Severity.Warning;
                    context.AddIssue(new RequirementIssue(issue.RuleId, $"[AI指摘] {issue.Message}", severity, TargetNodeId: issue.NodeId));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SemanticValidator Error] AI検証エラー: {ex.Message}");
            context.AddIssue(new RequirementIssue("SYS001", $"AI検証エラー: {ex.Message}", Severity.Error));
        }
    }

    // ==========================================
    // ヘルパーメソッド群
    // ==========================================
    private string BuildTreeText(IEnumerable<RequirementNode> nodes)
    {
        var sb = new StringBuilder();
        var rationales = nodes.Where(n => n.Type == UsdmType.Rationale).ToList();
        
        foreach (var rat in rationales)
        {
            var parentReq = nodes.FirstOrDefault(n => n.Id == rat.ParentId);
            sb.AppendLine($"【達成すべき目的】");
            sb.AppendLine($"- [理由] {rat.Description}");
            if (parentReq != null)
            {
                sb.AppendLine($"  (親要求: {parentReq.Description})");
            }
            sb.AppendLine($"  【上記目的を達成するための振る舞いと制約】");

            var childReqs = nodes.Where(n => n.ParentId == rat.Id && n.Type == UsdmType.ChildRequirement).ToList();
            foreach (var child in childReqs)
            {
                sb.AppendLine($"  - [子要求] {child.Description}");
                if (child.GherkinContext != null)
                {
                    sb.AppendLine($"    (BDD: Given {child.GherkinContext.Given}, When {child.GherkinContext.When}, Then {child.GherkinContext.Then})");
                }

                var specs = nodes.Where(n => n.ParentId == child.Id && n.Type == UsdmType.Specification).ToList();
                foreach (var spec in specs)
                {
                    sb.AppendLine($"    - [仕様] (NodeId: {spec.Id}) {spec.Description}");
                    if (spec.EarsContext != null)
                    {
                        sb.AppendLine($"      (EARS: [{spec.EarsContext.Pattern}] Trigger: {spec.EarsContext.Trigger}, Actor: {spec.EarsContext.Actor}, Response: {spec.EarsContext.Response})");
                    }
                }
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private HashSet<string> ExtractUsedNamespaces(IEnumerable<RequirementNode> nodes)
    {
        var namespaces = new HashSet<string>();
        foreach (var node in nodes)
        {
            var text = node.Description;
            if (node.EarsContext != null)
            {
                text += $" {node.EarsContext.Trigger} {node.EarsContext.Response}";
            }
            
            var matches = Regex.Matches(text, @"\[(.*?):.*?\]");
            foreach (Match match in matches)
            {
                namespaces.Add(match.Groups[1].Value);
            }
        }
        return namespaces;
    }

    private string ExtractJson(string text)
    {
        var pattern = "\\x60\\x60\\x60(?:json)?\\s*(\\[.*?\\])\\s*\\x60\\x60\\x60";
        var match = Regex.Match(text, pattern, RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value : text;
    }
}