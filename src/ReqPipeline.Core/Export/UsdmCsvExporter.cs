using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ReqPipeline.Core.Models;

namespace ReqPipeline.Core.Export;

public class UsdmCsvExporter : IRequirementExporter
{
    public void Export(string featureName, IEnumerable<RequirementNode> nodes, string outputDirectory)
    {
        var targetDir = Path.Combine(outputDirectory, "docs", "requirements");
        Directory.CreateDirectory(targetDir);
        using var writer = new StreamWriter(Path.Combine(targetDir, $"{featureName}_usdm.csv"), false, new UTF8Encoding(true));

        writer.WriteLine("親要求ID,親要求内容,理由内容,子要求内容,Given,When,Then,仕様内容,EARSパターン,Actor,Trigger,Response");

        var parents = nodes.Where(n => n.Type == UsdmType.ParentRequirement);
        foreach (var parent in parents)
        {
            var rationales = nodes.Where(n => n.ParentId == parent.Id && n.Type == UsdmType.Rationale).ToList();
            if (!rationales.Any()) { WriteRow(writer, parent.Id.ToString(), parent.Description, "", "", "", "", "", "", "", "", "", ""); continue; }

            foreach (var rat in rationales)
            {
                var children = nodes.Where(n => n.ParentId == rat.Id && n.Type == UsdmType.ChildRequirement).ToList();
                if (!children.Any()) { WriteRow(writer, parent.Id.ToString(), parent.Description, rat.Description, "", "", "", "", "", "", "", "", ""); continue; }

                foreach (var child in children)
                {
                    var gCtx = child.GherkinContext ?? new GherkinContext();
                    var specs = nodes.Where(n => n.ParentId == child.Id && n.Type == UsdmType.Specification).ToList();
                    if (!specs.Any()) { WriteRow(writer, parent.Id.ToString(), parent.Description, rat.Description, child.Description, gCtx.Given, gCtx.When, gCtx.Then, "", "", "", "", ""); continue; }

                    foreach (var spec in specs)
                    {
                        var eCtx = spec.EarsContext ?? new EarsContext();
                        WriteRow(writer, parent.Id.ToString().Substring(0,8), parent.Description, rat.Description, child.Description, gCtx.Given, gCtx.When, gCtx.Then, spec.Description, eCtx.Pattern, eCtx.Actor, eCtx.Trigger, eCtx.Response);
                    }
                }
            }
        }
    }

    private void WriteRow(StreamWriter writer, params string[] columns)
    {
        var escaped = columns.Select(c => string.IsNullOrEmpty(c) ? "" : $"\"{c.Replace("\"", "\"\"")}\"");
        writer.WriteLine(string.Join(",", escaped));
    }
}