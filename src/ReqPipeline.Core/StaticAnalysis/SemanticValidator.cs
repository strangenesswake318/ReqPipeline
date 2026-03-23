using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions; // 💡 新規追加: Regex用
using System.Threading.Tasks;
using ReqPipeline.Core.Interfaces;
using ReqPipeline.Core.Models;

namespace ReqPipeline.Core.StaticAnalysis;

// AIが返してくるJSONのスキーマ定義
public class SemanticIssueResult
{
    [JsonPropertyName("issues")] public List<SemanticIssue> Issues { get; set; } = new();
}

public class SemanticIssue
{
    [JsonPropertyName("ruleId")] public string RuleId { get; set; } = "";
    [JsonPropertyName("message")] public string Message { get; set; } = "";
    [JsonPropertyName("severity")] public string Severity { get; set; } = "Warning";
}

public class SemanticValidator : IRequirementStaticAnalysis
{
    private readonly ILlmClient _llmClient;

    public SemanticValidator(ILlmClient llmClient)
    {
        _llmClient = llmClient;
    }

    public async Task ValidateAsync(PipelineContext context)
    {
        if (context.HasFatalError()) return;

        var treeText = BuildTreeText(context.Nodes);

        // ==========================================
        // 💡 新規追加: 必要なドメイン知識（用語集）だけを抽出
        // ==========================================
        var usedNamespaces = ExtractUsedNamespaces(context.Nodes);
        
        // 使われているネームスペース、またはグローバル(SYS)の用語だけをフィルタリング
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
        

        // 1. プロンプトのテンプレートファイルを読み込む
        var promptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Prompts", "SemanticValidationPrompt.md");
        var promptTemplate = await File.ReadAllTextAsync(promptPath);

        // 2. プレースホルダー（{{...}}）を実際の変数（glossaryMd, treeText）に置換する
        var prompt = promptTemplate
            .Replace("{{GlossaryMd}}", glossaryMd.ToString())
            .Replace("{{TreeText}}", treeText.ToString());

        try
        {
            var responseJson = await _llmClient.GenerateTextAsync(prompt, 0.0f);

            if (string.IsNullOrWhiteSpace(responseJson)) return;

            // LLMがマークダウンのコードブロック(```json ... ```)で返してきた場合への対策
            var cleanJson = ExtractJson(responseJson);

            var semanticResult = JsonSerializer.Deserialize<SemanticIssueResult>(cleanJson);

            if (semanticResult?.Issues != null)
            {
                foreach (var issue in semanticResult.Issues)
                {
                    var severity = issue.Severity.Equals("Error", StringComparison.OrdinalIgnoreCase) ? Severity.Error : Severity.Warning;
                    context.AddIssue(new RequirementIssue(issue.RuleId, $"[AI指摘] {issue.Message}", severity));
                }
            }
        }
        catch (Exception ex)
        {
            context.AddIssue(new RequirementIssue("SYS001", $"AI検証エラー: {ex.Message}", Severity.Error));
        }
    }

    // ==========================================
    // 💡 4層構造対応 ＆ AIの脳内階層逆転
    // ==========================================
    private string BuildTreeText(IEnumerable<RequirementNode> nodes)
    {
        var sb = new StringBuilder();
        
        // 1. Validationの頂点である「理由」からスタートする
        var rationales = nodes.Where(n => n.Type == UsdmType.Rationale).ToList();
        
        foreach (var rat in rationales)
        {
            // 理由の親である「親要求（Epic）」を取得
            var parentReq = nodes.FirstOrDefault(n => n.Id == rat.ParentId);
            
            sb.AppendLine($"【達成すべき目的 (Validation基準)】");
            sb.AppendLine($"- [理由] {rat.Description}");
            if (parentReq != null)
            {
                sb.AppendLine($"  (背景となる親要求: {parentReq.Description})");
            }
            sb.AppendLine($"  【上記目的を達成するための振る舞いと制約】");

            // 理由にぶら下がる「子要求(Gherkin)」を取得
            var childReqs = nodes.Where(n => n.ParentId == rat.Id && n.Type == UsdmType.ChildRequirement).ToList();
            foreach (var child in childReqs)
            {
                sb.AppendLine($"  - [子要求] {child.Description}");
                if (child.GherkinContext != null)
                {
                    sb.AppendLine($"    (BDD: Given {child.GherkinContext.Given}, When {child.GherkinContext.When}, Then {child.GherkinContext.Then})");
                }

                // 子要求にぶら下がる「仕様(EARS)」を取得
                var specs = nodes.Where(n => n.ParentId == child.Id && n.Type == UsdmType.Specification).ToList();
                foreach (var spec in specs)
                {
                    sb.AppendLine($"    - [仕様] {spec.Description}");
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

    // ==========================================
    // 💡 新規追加: 仕様書からネームスペースを抽出する処理
    // ==========================================
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
                namespaces.Add(match.Groups[1].Value); // Namespace部分を取得
            }
        }
        return namespaces;
    }

    // 補助: LLMがJSONブロック記法で返してきても安全にパースするための関数
    private string ExtractJson(string text)
    {
        // バッククォートをエスケープ文字で表現し、C#の文法エラーとUIの表示崩れを完全に防ぎます
        var pattern = "\\x60\\x60\\x60(?:json)?\\s*(\\{.*?\\})\\s*\\x60\\x60\\x60";
        var match = Regex.Match(text, pattern, RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value : text;
    }

}
