using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ReqPipeline.Core.Interfaces;

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