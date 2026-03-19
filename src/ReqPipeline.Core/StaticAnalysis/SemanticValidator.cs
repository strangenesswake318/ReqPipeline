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
        // ==========================================

        var prompt = $@"
あなたは要求工学とUSDMの専門家です。以下の【要求ツリー】を読み、意味論的な「矛盾」や「目的と手段の不一致」を厳格にValidation（妥当性確認）してください。
業務ドメインの知識（その仕様がビジネスとして正しいか）は推測せず、あくまで「テキスト間の論理的な整合性」と「ドメイン知識（用語集）」を元に評価してください。

{glossaryMd}

【Validationの採点基準】
1. IntentMismatch (目的と手段の不一致)
   親ノードである「[理由]」が示唆する方向性と、子ノードである「[仕様]」の振る舞いが、意味的に逆行または乖離していませんか？
2. SpecificationConflict (仕様間の衝突)
   同じツリー内に存在する複数の「[仕様]」間で、同じトリガー（条件）に対する振る舞いが矛盾・衝突していませんか？

【要求ツリー】
{treeText}

【出力要件】
・矛盾や不一致がない場合は issues を空の配列にしてください。
・必ず対象となるノードの具体的な文章を引用して指摘してください。一般論は禁止です。
・以下のJSONスキーマに従って出力してください。
{{
  ""issues"": [
    {{
      ""ruleId"": ""IntentMismatch または SpecificationConflict"",
      ""message"": ""対象の文章を引用し、なぜ矛盾・不一致しているのか論理的に説明してください。"",
      ""severity"": ""Warning""
    }}
  ]
}}";

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

    private string BuildTreeText(IEnumerable<RequirementNode> nodes)
    {
        var sb = new StringBuilder();
        var reqs = nodes.Where(n => n.Type == UsdmType.Requirement);
        
        foreach (var req in reqs)
        {
            sb.AppendLine($"- [要求] {req.Description}");
            var rats = nodes.Where(n => n.ParentId == req.Id && n.Type == UsdmType.Rationale);
            
            foreach (var rat in rats)
            {
                sb.AppendLine($"  - [理由] {rat.Description}");
                var specs = nodes.Where(n => n.ParentId == rat.Id && n.Type == UsdmType.Specification);
                
                foreach (var spec in specs)
                {
                    sb.AppendLine($"    - [仕様] {spec.Description}");
                    // 💡 仕様のEARSコンテキストもAIに読ませるように追記
                    if (spec.EarsContext != null)
                    {
                        sb.AppendLine($"      (EARS: [{spec.EarsContext.Pattern}] Trigger: {spec.EarsContext.Trigger}, Actor: {spec.EarsContext.Actor}, Response: {spec.EarsContext.Response})");
                    }
                }
            }
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
