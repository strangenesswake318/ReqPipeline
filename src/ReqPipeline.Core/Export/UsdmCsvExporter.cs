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
        // 出力先: docs/requirements/
        var targetDir = Path.Combine(outputDirectory, "docs", "requirements");
        Directory.CreateDirectory(targetDir);

        var filePath = Path.Combine(targetDir, $"{featureName}_usdm.csv");
        
        // Excelで文字化けしないように BOM付き UTF-8 で出力
        using var writer = new StreamWriter(filePath, false, new UTF8Encoding(true));

        // ヘッダー行
        writer.WriteLine("要求ID,要求内容,理由,仕様内容,パターン,Actor,Trigger,Response");

        var reqNodes = nodes.Where(n => n.Type == UsdmType.Requirement);
        foreach (var req in reqNodes)
        {
            // 当該要求に紐づく理由(Rationale)を取得
            var rationales = nodes.Where(n => n.ParentId == req.Id && n.Type == UsdmType.Rationale).ToList();
            if (!rationales.Any())
            {
                // 理由がない場合は要求だけ出力
                WriteRow(writer, req.Id.ToString().Substring(0,8), req.Description, "", "", "", "", "", "");
                continue;
            }

            foreach (var rat in rationales)
            {
                // 当該理由に紐づく仕様(Specification)を取得
                var specs = nodes.Where(n => n.ParentId == rat.Id && n.Type == UsdmType.Specification).ToList();
                if (!specs.Any())
                {
                    // 仕様がない場合は理由まで出力
                    WriteRow(writer, req.Id.ToString().Substring(0,8), req.Description, rat.Description, "", "", "", "", "");
                    continue;
                }

                foreach (var spec in specs)
                {
                    // すべて揃っている完全な行を出力
                    var ctx = spec.EarsContext ?? new EarsContext();
                    WriteRow(writer, 
                        req.Id.ToString().Substring(0, 8), // IDは短縮して表示
                        req.Description, 
                        rat.Description, 
                        spec.Description, 
                        ctx.Pattern, 
                        ctx.Actor, 
                        ctx.Trigger, 
                        ctx.Response);
                }
            }
        }
    }

    // CSVエスケープ処理（カンマや改行が含まれる場合の保護）
    private void WriteRow(StreamWriter writer, params string[] columns)
    {
        var escaped = columns.Select(c => 
        {
            if (string.IsNullOrEmpty(c)) return "";
            if (c.Contains(",") || c.Contains("\"") || c.Contains("\n"))
            {
                return $"\"{c.Replace("\"", "\"\"")}\"";
            }
            return c;
        });
        writer.WriteLine(string.Join(",", escaped));
    }
}