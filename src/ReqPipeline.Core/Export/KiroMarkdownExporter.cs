using System.IO;
using System.Linq;
using System.Collections.Generic;
using ReqPipeline.Core.Models;

namespace ReqPipeline.Core.Export;

public class KiroMarkdownExporter : IRequirementExporter
{
    public void Export(string featureName, IEnumerable<RequirementNode> nodes, string outputDirectory)
    {
        var targetDir = Path.Combine(outputDirectory, ".kiro", "specs", featureName);
        Directory.CreateDirectory(targetDir);
        using var writer = new StreamWriter(Path.Combine(targetDir, "requirements.md"));

        writer.WriteLine($"# Feature: {featureName}\n\n## Requirements Tree\n");

        // ルートを ParentRequirement に変更
        var rootNodes = nodes.Where(n => n.ParentId == null && n.Type == UsdmType.ParentRequirement);
        foreach (var root in rootNodes) WriteNode(writer, root, nodes.ToList(), 0);
    }

    private void WriteNode(StreamWriter writer, RequirementNode node, List<RequirementNode> allNodes, int depth)
    {
        var indent = new string(' ', depth * 2);
        string prefix = node.Type switch {
            UsdmType.ParentRequirement => "- **[EPIC]**",
            UsdmType.Rationale => "- *[WHY]*",
            UsdmType.ChildRequirement => "- **[SCENARIO]**",
            UsdmType.Specification => "- **[RULE]**",
            _ => "-"
        };

        writer.WriteLine($"{indent}{prefix} {node.Description}");

        // 子要求(Gherkin)の出力
        if (node.Type == UsdmType.ChildRequirement && node.GherkinContext != null)
        {
            var ctx = node.GherkinContext;
            writer.WriteLine($"{indent}  > **BDD:** Given `{ctx.Given}` | When `{ctx.When}` | Then `{ctx.Then}`");
        }

        // 仕様(EARS)の出力
        if (node.Type == UsdmType.Specification && node.EarsContext != null)
        {
            var ctx = node.EarsContext;
            writer.WriteLine($"{indent}  > **EARS:** `[{ctx.Pattern}]` | Trigger: `{ctx.Trigger}` | Actor: `{ctx.Actor}` | Response: `{ctx.Response}`");
        }

        foreach (var child in allNodes.Where(n => n.ParentId == node.Id))
        {
            WriteNode(writer, child, allNodes, depth + 1);
        }
    }
}