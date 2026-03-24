namespace ReqPipeline.Core.StaticAnalysis;

public enum Severity { Error, Warning }

public record RequirementIssue(
    string RuleId, 
    string Message, 
    Severity Severity, 
    string? Suggestion = null,
    string TargetNodeId = null
);