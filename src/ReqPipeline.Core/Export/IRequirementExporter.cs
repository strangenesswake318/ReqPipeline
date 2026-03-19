using System.Collections.Generic;
using ReqPipeline.Core.Models;

namespace ReqPipeline.Core.Export;

/// <summary>
/// 要求データを出力（Markdown, CSVなど）するための共通インターフェース
/// </summary>
public interface IRequirementExporter
{
    void Export(string featureName, IEnumerable<RequirementNode> nodes, string outputDirectory);
}