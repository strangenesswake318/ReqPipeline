using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ReqPipeline.Core.Models;
using ReqPipeline.Core.Interfaces;
using System.Text.Encodings.Web;
using System.Text.Unicode;



namespace ReqPipeline.Core.Infrastructure;

public class JsonRequirementProvider : IRequirementProvider
{
    public IEnumerable<RequirementNode> Load(string sourcePath)
    {
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException($"要求ファイルが見つかりません: {sourcePath}");

        var json = File.ReadAllText(sourcePath);
        
        // JSONのキャメルケース(camelCase)をC#のプロパティ(PascalCase)に自動マッピング
        var options = new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() } 
        };

        var nodes = JsonSerializer.Deserialize<List<RequirementNode>>(json, options);
        return nodes ?? new List<RequirementNode>();
    }

    public void Save(IEnumerable<RequirementNode> nodes, string path)
    {
        // Gitで差分が見やすいようにインデントを付け、日本語がUnicodeエスケープされないように設定
        var options = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };
        
        var json = JsonSerializer.Serialize(nodes, options);
        File.WriteAllText(path, json);
    }
}