using System.Text;
using ReqPipeline.Core.Interfaces;
using ReqPipeline.Core.Models;

namespace ReqPipeline.Core.KnowledgeBase;

public class MarkdownKnowledgeBase : IKnowledgeBase
{
    private readonly string _knowledgeDirectory;

    public MarkdownKnowledgeBase(string knowledgeDirectory)
    {
        _knowledgeDirectory = knowledgeDirectory;
    }

    public async Task<string> GetRelevantKnowledgeAsync(PipelineContext context)
    {
        if (!Directory.Exists(_knowledgeDirectory))
        {
            return "※ナレッジベースのディレクトリが見つかりません。";
        }

        var sb = new StringBuilder();
        sb.AppendLine("# 山本修一郎先生の要求工学ナレッジ（参考資料）");

        // ディレクトリ内のすべてのMarkdownファイルを読み込む
        // 将来的には、context.Issues の内容に応じて読み込むファイルを選別する「セマンティック検索」に進化させます
        var files = Directory.GetFiles(_knowledgeDirectory, "*.md");
        
        foreach (var file in files)
        {
            var content = await File.ReadAllTextAsync(file);
            sb.AppendLine($"\n## ファイル名: {Path.GetFileName(file)}");
            sb.AppendLine(content);
        }

        return sb.ToString();
    }
}