namespace ReqPipeline.Core.Models;

public enum UsdmType
{
    ParentRequirement, // Layer 2: 親要求 (Epic/Flow) - 全体像
    Rationale,         // Layer 1: 理由 (Why) - Validationの頂点
    ChildRequirement,  // Layer 3: 子要求 (Scenario) - Gherkin構文(1動詞)
    Specification      // Layer 4: 仕様 (Rule) - EARS構文
}