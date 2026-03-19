using System.IO;
using System.Linq;
using ReqPipeline.Core.Models;

namespace ReqPipeline.Core.Export;

public class KiroMarkdownExporter : IRequirementExporter
{
    public void Export(string featureName, IEnumerable<RequirementNode> nodes, string outputDirectory)
    {
        // 出力先ディレクトリの確保（例: .kiro/specs/auth）
        var targetDir = Path.Combine(outputDirectory, ".kiro", "specs", featureName);
        Directory.CreateDirectory(targetDir);

        var filePath = Path.Combine(targetDir, "requirements.md");
        using var writer = new StreamWriter(filePath);

        writer.WriteLine($"# Feature: {featureName}");
        writer.WriteLine("");
        writer.WriteLine();
        writer.WriteLine("## Requirements Tree");
        writer.WriteLine();

        // ルートノード（ParentIdがnullのRequirement）から再帰的に書き出す
        var rootNodes = nodes.Where(n => n.ParentId == null && n.Type == UsdmType.Requirement);
        foreach (var root in rootNodes)
        {
            WriteNode(writer, root, nodes.ToList(), 0);
        }
    }

    private void WriteNode(StreamWriter writer, RequirementNode node, List<RequirementNode> allNodes, int depth)
    {
        var indent = new string(' ', depth * 2);
        
        // AIがパースしやすいようにタグ付け
        string prefix = node.Type switch
        {
            UsdmType.Requirement => "- **[REQ]**",
            UsdmType.Rationale => "- *[RAT]*",
            UsdmType.Specification => "- **[SPEC]**",
            _ => "-"
        };

        writer.WriteLine($"{indent}{prefix} {node.Description}");

        // 仕様(Specification)の場合は、EARSの構造化データもAI向けに出力
        if (node.Type == UsdmType.Specification && node.EarsContext != null && node.EarsContext.Pattern != "None")
        {
            var ctx = node.EarsContext;
            var earsIndent = new string(' ', (depth + 1) * 2);
            // 例: > Pattern: EventDriven | When: ログインボタン押下 | Actor: 認証サーバ | Response: 検証する
            writer.WriteLine($"{earsIndent}> **EARS:** `{ctx.Pattern}` | **When:** {ctx.Trigger} | **Actor:** {ctx.Actor} | **Response:** {ctx.Response}");
        }

        // 子ノードの再帰処理
        var children = allNodes.Where(n => n.ParentId == node.Id).ToList();
        foreach (var child in children)
        {
            WriteNode(writer, child, allNodes, depth + 1);
        }
    }
}