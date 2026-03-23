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

    [Fact]
    public async Task ValidateAsync_非親要求がルートノードの場合_STR001エラーが出ること()
    {
        // Arrange
        var rootId = Guid.NewGuid();

        var nodes = new List<RequirementNode>
        {
            // 💡 意図的なバグ: Rationale が ParentId = null でルートノードになっている
            new RequirementNode { Id = rootId, ParentId = null, Type = UsdmType.Rationale, Description = "Why" }
        };

        var context = new PipelineContext(nodes, new Glossary { Project = "TestProject", Entries = new List<GlossaryEntry>() });
        var verifier = new StructureVerifier();

        // Act
        await verifier.ValidateAsync(context);

        // Assert
        Assert.Contains(context.Issues, issue => issue.RuleId == "STR001");
    }

    [Fact]
    public async Task ValidateAsync_存在しない親IDを持つノードの場合_STR002エラーが出ること()
    {
        // Arrange
        var rootId = Guid.NewGuid();
        var nonExistentParentId = Guid.NewGuid();

        var nodes = new List<RequirementNode>
        {
            new RequirementNode { Id = rootId, ParentId = null, Type = UsdmType.ParentRequirement, Description = "Epic" },
            // 💡 意図的なバグ: ParentId が存在しないIDを指している
            new RequirementNode { Id = Guid.NewGuid(), ParentId = nonExistentParentId, Type = UsdmType.Rationale, Description = "Why" }
        };

        var context = new PipelineContext(nodes, new Glossary { Project = "TestProject", Entries = new List<GlossaryEntry>() });
        var verifier = new StructureVerifier();

        // Act
        await verifier.ValidateAsync(context);

        // Assert
        Assert.Contains(context.Issues, issue => issue.RuleId == "STR002");
    }

    [Fact]
    public async Task ValidateAsync_親要求が親を持つ場合_STR003エラーが出ること()
    {
        // Arrange
        var rootId = Guid.NewGuid();
        var childEpicId = Guid.NewGuid();

        var nodes = new List<RequirementNode>
        {
            new RequirementNode { Id = rootId, ParentId = null, Type = UsdmType.ParentRequirement, Description = "Root Epic" },
            // 💡 意図的なバグ: 親要求が親を持つ
            new RequirementNode { Id = childEpicId, ParentId = rootId, Type = UsdmType.ParentRequirement, Description = "Child Epic" }
        };

        var context = new PipelineContext(nodes, new Glossary { Project = "TestProject", Entries = new List<GlossaryEntry>() });
        var verifier = new StructureVerifier();

        // Act
        await verifier.ValidateAsync(context);

        // Assert
        Assert.Contains(context.Issues, issue => issue.RuleId == "STR003");
    }

    [Fact]
    public async Task ValidateAsync_理由の親が親要求ではない場合_STR004エラーが出ること()
    {
        // Arrange
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var ratId = Guid.NewGuid();

        var nodes = new List<RequirementNode>
        {
            new RequirementNode { Id = rootId, ParentId = null, Type = UsdmType.ParentRequirement, Description = "Epic" },
            new RequirementNode { Id = childId, ParentId = rootId, Type = UsdmType.ChildRequirement, Description = "Scenario" },
            // 💡 意図的なバグ: Rationale の親が ChildRequirement になっている
            new RequirementNode { Id = ratId, ParentId = childId, Type = UsdmType.Rationale, Description = "Why" }
        };

        var context = new PipelineContext(nodes, new Glossary { Project = "TestProject", Entries = new List<GlossaryEntry>() });
        var verifier = new StructureVerifier();

        // Act
        await verifier.ValidateAsync(context);

        // Assert
        Assert.Contains(context.Issues, issue => issue.RuleId == "STR004");
    }

    [Fact]
    public async Task ValidateAsync_子要求の親が理由ではない場合_STR005エラーが出ること()
    {
        // Arrange
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var nodes = new List<RequirementNode>
        {
            new RequirementNode { Id = rootId, ParentId = null, Type = UsdmType.ParentRequirement, Description = "Epic" },
            // 💡 意図的なバグ: ChildRequirement の親が ParentRequirement になっている
            new RequirementNode { Id = childId, ParentId = rootId, Type = UsdmType.ChildRequirement, Description = "Scenario" }
        };

        var context = new PipelineContext(nodes, new Glossary { Project = "TestProject", Entries = new List<GlossaryEntry>() });
        var verifier = new StructureVerifier();

        // Act
        await verifier.ValidateAsync(context);

        // Assert
        Assert.Contains(context.Issues, issue => issue.RuleId == "STR005");
    }

    [Fact]
    public async Task ValidateAsync_仕様の親が子要求ではない場合_STR007エラーが出ること()
    {
        // Arrange
        var rootId = Guid.NewGuid();
        var ratId = Guid.NewGuid();
        var specId = Guid.NewGuid();

        var nodes = new List<RequirementNode>
        {
            new RequirementNode { Id = rootId, ParentId = null, Type = UsdmType.ParentRequirement, Description = "Epic" },
            new RequirementNode { Id = ratId, ParentId = rootId, Type = UsdmType.Rationale, Description = "Why" },
            // 💡 意図的なバグ: Specification の親が Rationale になっている
            new RequirementNode { Id = specId, ParentId = ratId, Type = UsdmType.Specification, Description = "Rule", EarsContext = new EarsContext() }
        };

        var context = new PipelineContext(nodes, new Glossary { Project = "TestProject", Entries = new List<GlossaryEntry>() });
        var verifier = new StructureVerifier();

        // Act
        await verifier.ValidateAsync(context);

        // Assert
        Assert.Contains(context.Issues, issue => issue.RuleId == "STR007");
    }

    [Fact]
    public async Task ValidateAsync_仕様にEARSコンテキストがない場合_STR008エラーが出ること()
    {
        // Arrange
        var rootId = Guid.NewGuid();
        var ratId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var specId = Guid.NewGuid();

        var nodes = new List<RequirementNode>
        {
            new RequirementNode { Id = rootId, ParentId = null, Type = UsdmType.ParentRequirement, Description = "Epic" },
            new RequirementNode { Id = ratId, ParentId = rootId, Type = UsdmType.Rationale, Description = "Why" },
            new RequirementNode { Id = childId, ParentId = ratId, Type = UsdmType.ChildRequirement, Description = "Scenario", GherkinContext = new GherkinContext() },
            // 💡 意図的なバグ: Specification なのに EarsContext を null にする
            new RequirementNode { Id = specId, ParentId = childId, Type = UsdmType.Specification, Description = "Rule", EarsContext = null }
        };

        var context = new PipelineContext(nodes, new Glossary { Project = "TestProject", Entries = new List<GlossaryEntry>() });
        var verifier = new StructureVerifier();

        // Act
        await verifier.ValidateAsync(context);

        // Assert
        Assert.Contains(context.Issues, issue => issue.RuleId == "STR008");
    }
}
