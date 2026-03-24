using System.Collections.Generic;
using System.Linq;
using ReqPipeline.Core.StaticAnalysis;

namespace ReqPipeline.Core.Models;

public class PipelineContext
{
    public IEnumerable<RequirementNode> Nodes { get; } = new List<RequirementNode>();
    public Glossary Glossary { get; } = new Glossary();
    public List<RequirementIssue> Issues { get; } = new();

    public PipelineContext(IEnumerable<RequirementNode> nodes, Glossary glossary)
    {
        Nodes = nodes;
        Glossary = glossary;
    }

    public PipelineContext()
    {
    }

    // エラー（Linterによる致命的な構文エラー等）が含まれているか確認する便利メソッド
    public bool HasFatalError() => Issues.Any(i => i.Severity == Severity.Error);
    
    // 検証結果を追加する
    public void AddIssue(RequirementIssue issue) => Issues.Add(issue);
    public void AddIssues(IEnumerable<RequirementIssue> issues) => Issues.AddRange(issues);
}