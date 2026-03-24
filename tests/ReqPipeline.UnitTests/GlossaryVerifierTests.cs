using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReqPipeline.Core.Models;
using ReqPipeline.Core.StaticAnalysis;

namespace ReqPipeline.UnitTests;

public class GlossaryVerifierTests
{
    [Fact]
    public async Task ValidateAsync_登録済み用語のみ使用の場合_エラーが出ないこと()
    {
        // Arrange
        var rootId = Guid.NewGuid();
        var ratId = Guid.NewGuid();
        var specId = Guid.NewGuid();

        // 💡 登録済みのネームスペース付き用語
        var registeredTerm = "[SYS:ValidAction]";
        var registeredActor = "[SYS:ValidActor]";

        var nodes = new List<RequirementNode>
        {
            new RequirementNode { Id = rootId, ParentId = null, Type = UsdmType.ParentRequirement, Description = "Root Req" },
            new RequirementNode { Id = ratId, ParentId = rootId, Type = UsdmType.Rationale, Description = "Rationale" },
            new RequirementNode
            {
                Id = specId,
                ParentId = ratId,
                Type = UsdmType.Specification,
                Description = "システムは[SYS:ValidAction]を実行する",
                EarsContext = new EarsContext
                {
                    Pattern = "Ubiquitous",
                    Actor = registeredActor,
                    Trigger = "常に",
                    Response = registeredTerm
                }
            }
        };

        var glossary = new Glossary
        {
            Project = "Test",
            Entries = new List<GlossaryEntry>
            {
                new GlossaryEntry { Namespace = "SYS", Term = "ValidAction" },
                new GlossaryEntry { Namespace = "SYS", Term = "ValidActor" }
            }
        };

        var context = new PipelineContext(nodes, glossary);
        var verifier = new GlossaryVerifier();

        // Act
        await verifier.ValidateAsync(context);

        // Assert
        Assert.Empty(context.Issues);
    }

    [Fact]
    public async Task ValidateAsync_未登録の用語を使用の場合_GLOS002エラーが出ること()
    {
        // Arrange
        var rootId = Guid.NewGuid();
        var ratId = Guid.NewGuid();
        var specId = Guid.NewGuid();

        // 💡 未登録の用語を含むEarsContext
        var unregisteredTerm = "[SYS:UnknownEvent]";

        var nodes = new List<RequirementNode>
        {
            new RequirementNode { Id = rootId, ParentId = null, Type = UsdmType.ParentRequirement, Description = "Root Req" },
            new RequirementNode { Id = ratId, ParentId = rootId, Type = UsdmType.Rationale, Description = "Rationale" },
            new RequirementNode
            {
                Id = specId,
                ParentId = ratId,
                Type = UsdmType.Specification,
                Description = "システムは何かを実行する",
                EarsContext = new EarsContext
                {
                    Pattern = "EventDriven",
                    Actor = "[SYS:KnownActor]", // これは登録済みとする
                    Trigger = unregisteredTerm, // 💡 これが未登録
                    Response = "[SYS:KnownResponse]" // これも登録済みとする
                }
            }
        };

        var glossary = new Glossary
        {
            Project = "Test",
            Entries = new List<GlossaryEntry>
            {
                new GlossaryEntry { Namespace = "SYS", Term = "KnownActor" },
                new GlossaryEntry { Namespace = "SYS", Term = "KnownResponse" }
            }
        };

        var context = new PipelineContext(nodes, glossary);
        var verifier = new GlossaryVerifier();

        // Act
        await verifier.ValidateAsync(context);

        // Assert
        Assert.Contains(context.Issues, issue => issue.RuleId == "GLOS-002");
    }

    [Fact]
    public async Task ValidateAsync_否定表現が含まれる場合_GLOS003警告が出ること()
    {
        // Arrange
        var rootId = Guid.NewGuid();
        var ratId = Guid.NewGuid();
        var specId = Guid.NewGuid();

        var nodes = new List<RequirementNode>
        {
            new RequirementNode { Id = rootId, ParentId = null, Type = UsdmType.ParentRequirement, Description = "Root Req" },
            new RequirementNode { Id = ratId, ParentId = rootId, Type = UsdmType.Rationale, Description = "Rationale" },
            new RequirementNode
            {
                Id = specId,
                ParentId = ratId,
                Type = UsdmType.Specification,
                Description = "システムは処理を行わないこと", // 💡 否定表現
                EarsContext = new EarsContext
                {
                    Pattern = "Ubiquitous",
                    Actor = "[SYS:Actor]",
                    Trigger = "常に",
                    Response = "何も実行しない" // 💡 こちらも否定表現
                }
            }
        };

        var glossary = new Glossary
        {
            Project = "Test",
            Entries = new List<GlossaryEntry>
            {
                new GlossaryEntry { Namespace = "SYS", Term = "Actor" },
                new GlossaryEntry { Namespace = "SYS", Term = "何も実行しない" }
            }
        };

        var context = new PipelineContext(nodes, glossary);
        var verifier = new GlossaryVerifier();

        // Act
        await verifier.ValidateAsync(context);

        // Assert
        Assert.Contains(context.Issues, issue => issue.RuleId == "GLOS-003" && issue.Severity == Severity.Warning);
    }
}
