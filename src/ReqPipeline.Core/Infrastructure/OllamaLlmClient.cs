using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ReqPipeline.Core.Interfaces;
using System.IO; // Path や File のため
using System.Text.Json.Serialization; // ReferenceHandler のため
using ReqPipeline.Core.Models; // PipelineContext のため

namespace ReqPipeline.Core.Infrastructure;

public class OllamaLlmClient : ILlmClient
{
    private readonly HttpClient _httpClient = new();
    private const string OllamaEndpoint = "http://localhost:11434/api/generate";
    private readonly string _modelName;

    // コンストラクタでモデル名を注入できるようにしておく（テストや拡張のため）
    public OllamaLlmClient(string modelName = "qwen2.5:7b")
    {
        _modelName = modelName;
    }

    // 【追加】山本メソッドRAG用のレビューメソッド
    public async Task<string> ReviewWithKnowledgeAsync(PipelineContext context, string knowledge)
    {
        // 1. 先ほど作った RAG 用のプロンプトテンプレートを読み込む
        var promptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Prompts", "SystemReviewPrompt.md");
        var promptTemplate = await File.ReadAllTextAsync(promptPath);

        // 2. 要求仕様（ツリー構造）をAIが読めるようにJSON文字列化する
        // ※ノードの親子関係による「循環参照エラー」を防ぐための魔法の設定です
        var jsonOptions = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles 
        };
        var requirementsJson = JsonSerializer.Serialize(context.Nodes, jsonOptions);

        // 3. プレースホルダーに「山本メソッドの知見」と「要求仕様」をガッチャンコ！
        var finalPrompt = promptTemplate
            .Replace("{{KnowledgeBase}}", knowledge)
            .Replace("{{Requirements}}", requirementsJson);

            // 💡 3-1. AIに送る直前の「完全体プロンプト」をターミナルに表示！
            Console.WriteLine("======================================");
            Console.WriteLine("🚀 【AI送信プロンプト】");
            Console.WriteLine(finalPrompt);
            Console.WriteLine("======================================");

            var rawResponse = await GenerateTextAsync(finalPrompt, 0.0f);

            // 💡 3-2. AIから返ってきた「生の回答」をターミナルに表示！
            Console.WriteLine("======================================");
            Console.WriteLine("🤖 【AI生レスポンス】");
            Console.WriteLine(rawResponse);
            Console.WriteLine("======================================");

        // 4. 完成したプロンプトを、既存の激ツヨメソッド（GenerateTextAsync）に投げる！
        // レビューなので、temperature は 0.0f (最も堅実で論理的な回答) を指定します
        return await GenerateTextAsync(finalPrompt, 0.0f);
    }

    public async Task<string> GenerateTextAsync(string prompt, float temperature = 0.0f)
    {
        // Ollama特有のリクエスト形式
        var requestBody = new
        {
            model = _modelName,
            prompt = prompt,
            format = "json",
            stream = false,
            options = new Dictionary<string, object> { { "temperature", temperature } }
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(OllamaEndpoint, content);
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();
        
        // JsonDocumentを使ってOllamaのレスポンスから "response" フィールドだけを抜き出す
        using var jsonDoc = JsonDocument.Parse(responseString);
        var rawText = jsonDoc.RootElement.GetProperty("response").GetString() ?? "";

        // AI特有のMarkdownゴミ取り（クリーニング処理もインフラの責任に押し付ける）
        rawText = rawText.Trim();
        if (rawText.StartsWith("```json", StringComparison.OrdinalIgnoreCase)) rawText = rawText.Substring(7);
        if (rawText.EndsWith("```")) rawText = rawText.Substring(0, rawText.Length - 3);

        return rawText.Trim();
    }
}