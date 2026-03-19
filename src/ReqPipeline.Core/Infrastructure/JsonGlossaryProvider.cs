using System.IO;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using ReqPipeline.Core.Interfaces;
using ReqPipeline.Core.Models;

namespace ReqPipeline.Core.Infrastructure;

public class JsonGlossaryProvider : IGlossaryProvider
{
    public Glossary Load(string path)
    {
        if (!File.Exists(path)) return new Glossary();
        
        var json = File.ReadAllText(path);
        try
        {
            // 新しい { "Project": "...", "Entries": [...] } の形式として読み込む
            return JsonSerializer.Deserialize<Glossary>(json) ?? new Glossary();
        }
        catch
        {
            // ※もし古い配列形式の glossary.json が残っていた場合のフォールバック（あるいは単に空を返す）
            return new Glossary(); 
        }
    }

    public void Save(Glossary glossary, string path)
    {
        var options = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };
        
        var json = JsonSerializer.Serialize(glossary, options);
        File.WriteAllText(path, json);
    }
}