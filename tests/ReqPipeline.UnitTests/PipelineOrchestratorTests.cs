using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using ReqPipeline.Core.Models;
using ReqPipeline.Core.StaticAnalysis;
using ReqPipeline.Core.Interfaces;
using ReqPipeline.Core.Export;
using ReqPipeline.Core.Application; // PipelineOrchestratorのネームスペース

namespace ReqPipeline.UnitTests;

public class PipelineOrchestratorTests
{
    [Fact]
    public async Task RunPipelineAsync_Linterエラーがない場合_Exporterが実行されること()
    {
        // Arrange (準備)
        var mockReqProvider = new Mock<IRequirementProvider>();
        var mockGlosProvider = new Mock<IGlossaryProvider>();
        var mockAnalyzer = new Mock<IRequirementStaticAnalysis>();
        var mockExporter = new Mock<IRequirementExporter>();

        // 1. プロバイダーの影武者に「Loadが呼ばれたら空のデータを返す」よう仕込む
        mockReqProvider.Setup(x => x.Load(It.IsAny<string>())).Returns(new List<RequirementNode>());
        mockGlosProvider.Setup(x => x.Load(It.IsAny<string>())).Returns(new Glossary { Project = "Test", Entries = new List<GlossaryEntry>() });

        var orchestrator = new PipelineOrchestrator(
            mockReqProvider.Object,
            mockGlosProvider.Object,
            new List<IRequirementStaticAnalysis> { mockAnalyzer.Object },
            new List<IRequirementExporter> { mockExporter.Object }
        );

        // Act (実行)
        // メソッド名と引数を実際の実装に合わせる！
        await orchestrator.RunPipelineAsync("dummyReqPath", "dummyGlosPath", "dummyOutputDir");

        // Assert (検証)
        // エクスポートが1回呼ばれたことを確認 (引数は "feature_auto", 任意のノードリスト, "dummyOutputDir")
        mockExporter.Verify(x => x.Export("feature_auto", It.IsAny<IEnumerable<RequirementNode>>(), "dummyOutputDir"), Times.Once);
    }

    [Fact]
    public async Task RunPipelineAsync_Linterで致命的エラーが出た場合_Exporterがスキップされること()
    {
        // Arrange
        var mockReqProvider = new Mock<IRequirementProvider>();
        var mockGlosProvider = new Mock<IGlossaryProvider>();
        var mockAnalyzer = new Mock<IRequirementStaticAnalysis>();
        var mockExporter = new Mock<IRequirementExporter>();

        mockReqProvider.Setup(x => x.Load(It.IsAny<string>())).Returns(new List<RequirementNode>());
        mockGlosProvider.Setup(x => x.Load(It.IsAny<string>())).Returns(new Glossary { Project = "Test", Entries = new List<GlossaryEntry>() });

        // 💡 2. 影武者のLinter（Analyzer）が動いた瞬間に、意図的に致命的エラーをコンテキストに混入させる！
        mockAnalyzer.Setup(x => x.ValidateAsync(It.IsAny<PipelineContext>()))
            .Callback<PipelineContext>(ctx => 
            {
                // 先ほど修正した Guid -> string への変換（.ToString()）も反映済みです
                ctx.AddIssue(new RequirementIssue("SYS001", "致命的なエラー", Severity.Error, Guid.NewGuid().ToString()));
            })
            .Returns(Task.CompletedTask);

        var orchestrator = new PipelineOrchestrator(
            mockReqProvider.Object,
            mockGlosProvider.Object,
            new List<IRequirementStaticAnalysis> { mockAnalyzer.Object },
            new List<IRequirementExporter> { mockExporter.Object }
        );

        // Act
        await orchestrator.RunPipelineAsync("dummyReqPath", "dummyGlosPath", "dummyOutputDir");

        // Assert
        // エラーがあるので、Exporterの Export メソッドは【1度も呼ばれていない】ことを証明する！
        mockExporter.Verify(x => x.Export(It.IsAny<string>(), It.IsAny<IEnumerable<RequirementNode>>(), It.IsAny<string>()), Times.Never);
    }
}