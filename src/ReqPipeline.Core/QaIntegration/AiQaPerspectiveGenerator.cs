using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ReqPipeline.Core.Interfaces;
using ReqPipeline.Core.Models;

namespace ReqPipeline.Core.QaIntegration;

public class AiQaPerspectiveGenerator
{
    private readonly ILlmClient _llmClient;

    public AiQaPerspectiveGenerator(ILlmClient _llmClient)
    {
        this._llmClient = _llmClient;
    }

    public async Task<List<QaPerspective>> GeneratePerspectivesAsync(RequirementNode specNode, Glossary glossary)
    {
        Console.WriteLine($"[DEBUG] QA分析開始: NodeId={specNode.Id}");

        if (specNode.Type != UsdmType.Specification || specNode.EarsContext == null)
        {
            Console.WriteLine("[DEBUG] エラー: 仕様ノードではないか、EARSコンテキストが空です。");
            return new List<QaPerspective>();
        }

        var text = $"{specNode.Description} {specNode.EarsContext.Trigger} {specNode.EarsContext.Response}";
        var namespaces = new HashSet<string>();
        foreach (Match match in Regex.Matches(text, @"\[(.*?):.*?\]"))
        {
            namespaces.Add(match.Groups[1].Value);
        }

        var relevantGlossary = glossary.Entries
            .Where(e => namespaces.Contains(e.Namespace) || e.Namespace == "SYS")
            .ToList();

        var glossaryMd = new StringBuilder();
        foreach (var entry in relevantGlossary)
        {
            glossaryMd.AppendLine($"- **{entry.FullName}** ({entry.Category}): {entry.Definition}");
        }

        var prompt = $@"
あなたはJSTQB認定のシニア・テストアナリストです。
以下の仕様からテスト観点を抽出してください。必ず指定のJSON形式で返答してください。

【用語集】
{glossaryMd}

【対象仕様】
記述: {specNode.Description}
EARS: [{specNode.EarsContext.Pattern}] Trigger: {specNode.EarsContext.Trigger}, Actor: {specNode.EarsContext.Actor}, Response: {specNode.EarsContext.Response}

【出力形式】
{{
  ""perspectives"": [
    {{ ""category"": ""カテゴリ"", ""viewpoint"": ""観点"", ""description"": ""理由"" }}
  ]
}}";

        try 
        {
            var responseJson = await _llmClient.GenerateTextAsync(prompt, 0.2f);
            
            // 💡 重要: AIの生の回答をコンソールに出力
            Console.WriteLine("--- AI RAW RESPONSE START ---");
            Console.WriteLine(responseJson);
            Console.WriteLine("--- AI RAW RESPONSE END ---");

            if (string.IsNullOrWhiteSpace(responseJson)) return new List<QaPerspective>();

            var cleanJson = ExtractJson(responseJson);
            var result = JsonSerializer.Deserialize<QaPerspectiveResult>(cleanJson);
            
            Console.WriteLine($"[DEBUG] パース成功: {result?.Perspectives.Count ?? 0} 件の観点を取得");
            return result?.Perspectives ?? new List<QaPerspective>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] AI生成またはパース中に例外発生: {ex.Message}");
            throw; // Home.razor側でキャッチさせる
        }
    }

    private string ExtractJson(string text)
    {
        // 以前の途切れ対策済み正規表現
        var pattern = "\\x60\\x60\\x60(?:json)?\\s*(\\{.*?\\})\\s*\\x60\\x60\\x60";
        var match = Regex.Match(text, pattern, RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value : text;
    }
}