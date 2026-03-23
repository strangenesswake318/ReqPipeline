using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using ReqPipeline.Core.Models;
using ReqPipeline.Core.StaticAnalysis;

namespace ReqPipeline.UnitTests;

public class StructureVerifierTests
{
    [Fact]
    public async Task ValidateAsync_完璧な4層構造の場合_エラーが出ないこと()
    {
        // Arrange (準備)
        var rootId = Guid.NewGuid();
        var ratId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var specId = Guid.NewGuid();

        var nodes = new List<RequirementNode>
        {
            new RequirementNode { Id = rootId, ParentId = null, Type = UsdmType.ParentRequirement, Description = "Epic" },
            new RequirementNode { Id = ratId, ParentId = rootId, Type = UsdmType.Rationale, Description = "Why" },
            new RequirementNode { Id = childId, ParentId = ratId, Type = UsdmType.ChildRequirement, Description = "Scenario", GherkinContext = new GherkinContext() },
            new RequirementNode { Id = specId, ParentId = childId, Type = UsdmType.Specification, Description = "Rule", EarsContext = new EarsContext() }
        };

        var context = new PipelineContext(nodes, new Glossary { Project = "TestProject", Entries = new List<GlossaryEntry>() });
        var verifier = new StructureVerifier();

        // Act (実行)
        await verifier.ValidateAsync(context);

        // Assert (検証)
        Assert.Empty(context.Issues); // 問題（Issue）が0件であること！
    }

    [Fact]
    public async Task ValidateAsync_子要求にGherkinがない場合_STR006エラーが出ること()
    {
        // Arrange
        var rootId = Guid.NewGuid();
        var ratId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var nodes = new List<RequirementNode>
        {
            new RequirementNode { Id = rootId, ParentId = null, Type = UsdmType.ParentRequirement },
            new RequirementNode { Id = ratId, ParentId = rootId, Type = UsdmType.Rationale },
            
            // 💡 意図的なバグ: ChildRequirement なのに GherkinContext を null にする
            new RequirementNode { Id = childId, ParentId = ratId, Type = UsdmType.ChildRequirement, GherkinContext = null }
        };

        var context = new PipelineContext(nodes, new Glossary { Project = "TestProject", Entries = new List<GlossaryEntry>() });
        var verifier = new StructureVerifier();

        // Act
        await verifier.ValidateAsync(context);

        // Assert
        // Issuesの中に、RuleIdが "STR006" のエラーが含まれていること！
        Assert.Contains(context.Issues, issue => issue.RuleId == "STR006");
    }
}
