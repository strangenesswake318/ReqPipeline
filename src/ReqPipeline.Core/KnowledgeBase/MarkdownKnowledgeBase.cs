using System.Text;
using ReqPipeline.Core.Interfaces;
using ReqPipeline.Core.Models;
using ReqPipeline.Core.Utils;


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
        // 💡 呪文を唱えるだけで、どこにあっても絶対に見つけてくる！
        var folderName = new DirectoryInfo(_knowledgeDirectory).Name;
        var targetDir = ResourceLocator.FindDirectory(folderName);

        if (!Directory.Exists(targetDir))
        {
            return $"※ナレッジベースが見つかりません。(探したフォルダ名: {folderName})";
        }

        var sb = new StringBuilder();
        sb.AppendLine("# 山本修一郎先生の要求工学ナレッジ（参考資料）");

        var files = Directory.GetFiles(targetDir, "*.md");
        foreach (var file in files)
        {
            var content = await File.ReadAllTextAsync(file);
            sb.AppendLine($"\n## ファイル名: {Path.GetFileName(file)}");
            sb.AppendLine(content);
        }

        return sb.ToString();
    }
}