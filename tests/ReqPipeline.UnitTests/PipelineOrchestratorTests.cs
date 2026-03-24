using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using ReqPipeline.Core.Models;
using ReqPipeline.Core.Interfaces;
using ReqPipeline.Core.Application;
using ReqPipeline.Core.StaticAnalysis; // ※PipelineOrchestratorのネームスペース


namespace ReqPipeline.UnitTests;

public class PipelineOrchestratorTests
{
    [Fact]
    public virtual async Task RunPipelineAsync_エラーがない場合_全ての検証が順番に実行されること()
    {
        // Arrange (準備)
        // 1. 今のOrchestratorが必要とする「3つの演奏者（Validator）」の影武者だけを用意！
        var mockStructureVerifier = new Mock<StructureVerifier>();
        var mockGlossaryVerifier = new Mock<GlossaryVerifier>();
        var mockSemanticValidator = new Mock<IRequirementStaticAnalysis>();

        var orchestrator = new PipelineOrchestrator(
            mockStructureVerifier.Object,
            mockGlossaryVerifier.Object,
            mockSemanticValidator.Object
        );

        // 💡 実行に必要なコンテキスト（楽譜）を用意する
        var context = new PipelineContext(new List<RequirementNode>(), new Glossary());

        // Act (実行)
        // 今のOrchestratorは、コンテキストを1つ渡すだけで動きます！
        await orchestrator.RunPipelineAsync(context);

        // Assert (検証)
        // エラーがないので、3つの検証クラスがすべて「1回ずつ」呼ばれたことを証明する！
        mockStructureVerifier.Verify(x => x.ValidateAsync(context), Times.Once);
        mockGlossaryVerifier.Verify(x => x.ValidateAsync(context), Times.Once);
        mockSemanticValidator.Verify(x => x.ValidateAsync(context), Times.Once);
    }

    [Fact]
    public async Task RunPipelineAsync_構造検証で致命的エラーが出た場合_以降の検証がスキップされること()
    {
        // Arrange
        var mockStructureVerifier = new Mock<StructureVerifier>();
        var mockGlossaryVerifier = new Mock<GlossaryVerifier>();
        var mockSemanticValidator = new Mock<IRequirementStaticAnalysis>();

        // 💡 影武者の第1奏者（構造検証）が動いた瞬間に、意図的に致命的エラーを発生させる！
        mockStructureVerifier.Setup(x => x.ValidateAsync(It.IsAny<PipelineContext>()))
            .Callback<PipelineContext>(ctx => 
            {
                ctx.AddIssue(new RequirementIssue("SYS001", "構造が崩れています！", Severity.Error, Guid.NewGuid().ToString()));
            })
            .Returns(Task.CompletedTask);

        var orchestrator = new PipelineOrchestrator(
            mockStructureVerifier.Object,
            mockGlossaryVerifier.Object,
            mockSemanticValidator.Object
        );

        var context = new PipelineContext(new List<RequirementNode>(), new Glossary());

        // Act
        await orchestrator.RunPipelineAsync(context);

        // Assert
        // 第1奏者（構造検証）は呼ばれたことを確認
        mockStructureVerifier.Verify(x => x.ValidateAsync(context), Times.Once);
        
        // 🛑 致命的エラーが出たので、第2、第3奏者（用語集・AI）は【1度も呼ばれていない】ことを証明する！
        mockGlossaryVerifier.Verify(x => x.ValidateAsync(It.IsAny<PipelineContext>()), Times.Never);
        mockSemanticValidator.Verify(x => x.ValidateAsync(It.IsAny<PipelineContext>()), Times.Never);
    }
}