using System;
using System.Collections.Generic;
using System.Threading.Tasks;
//using ReqPipeline.Core.Data;
using ReqPipeline.Core.Interfaces;
using ReqPipeline.Core.Models;
using ReqPipeline.Core.StaticAnalysis;
using ReqPipeline.Core.Export;


namespace ReqPipeline.Core.Application;

public class PipelineOrchestrator
{
    private readonly IRequirementProvider _reqProvider;
    private readonly IGlossaryProvider _glosProvider;
    private readonly IEnumerable<IRequirementStaticAnalysis> _validators;
    private readonly IEnumerable<IRequirementExporter> _exporters; // ← 【追加】エクスポートも引き受ける

    public PipelineOrchestrator(
        IRequirementProvider reqProvider,
        IGlossaryProvider glosProvider,
        IEnumerable<IRequirementStaticAnalysis> validators,
        IEnumerable<IRequirementExporter> exporters) // ← 【追加】
    {
        _reqProvider = reqProvider;
        _glosProvider = glosProvider;
        _validators = validators;
        _exporters = exporters;
    }

    // 引数に「出力先ディレクトリ」を追加
    public async Task<PipelineContext> RunPipelineAsync(string reqPath, string glosPath, string outputDir)
    {
        var nodes = _reqProvider.Load(reqPath);
        var glossary = _glosProvider.Load(glosPath);
        var context = new PipelineContext(nodes, glossary);

        // 1. バリデーションフェーズ
        foreach (var validator in _validators)
        {
            await validator.ValidateAsync(context);
            if (context.HasFatalError()) break; // 致命的エラーでストップ
        }

        // 2. エクスポートフェーズ（致命的エラーがなければ全エクスポーターを回す）
        if (!context.HasFatalError())
        {
            foreach (var exporter in _exporters)
            {
                // プロジェクト名 "feature_auto" は一旦固定にしていますが、将来的には引数化できます
                exporter.Export("feature_auto", context.Nodes, outputDir);
            }
        }

        return context;
    }
}