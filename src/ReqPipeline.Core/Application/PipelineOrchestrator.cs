using System;
using System.Collections.Generic;
using System.Text.Json; // 💡 追加
using System.Threading.Tasks;
using ReqPipeline.Core.Interfaces;
using ReqPipeline.Core.Models;
using ReqPipeline.Core.StaticAnalysis;
using ReqPipeline.Core.Export;

namespace ReqPipeline.Core.Application;

public class PipelineOrchestrator
{
    private readonly IRequirementProvider _reqProvider;
    private readonly IGlossaryProvider _glosProvider;
    private readonly IKnowledgeBase _knowledgeBase;
    private readonly IEnumerable<IRequirementStaticAnalysis> _validators;
    private readonly IEnumerable<IRequirementExporter> _exporters;
    private readonly ILlmClient _llmClient; // 💡 【追加】AIクライアント

    public PipelineOrchestrator(
        IRequirementProvider reqProvider,
        IGlossaryProvider glosProvider,
        IKnowledgeBase knowledgeBase,
        IEnumerable<IRequirementStaticAnalysis> validators,
        IEnumerable<IRequirementExporter> exporters,
        ILlmClient llmClient) // 💡 【追加】
    {
        _reqProvider = reqProvider;
        _glosProvider = glosProvider;
        _knowledgeBase = knowledgeBase;
        _validators = validators;
        _exporters = exporters;
        _llmClient = llmClient; // 💡 【追加】
    }

    public async Task<PipelineContext> RunPipelineAsync(string reqPath, string glosPath, string outputDir)
    {
        var nodes = _reqProvider.Load(reqPath);
        var glossary = _glosProvider.Load(glosPath);
        var context = new PipelineContext(nodes, glossary);

        // 1. バリデーションフェーズ (Linter)
        foreach (var validator in _validators)
        {
            await validator.ValidateAsync(context);
            if (context.HasFatalError()) break; 
        }

        // 2. 山本メソッドRAG ＆ AIレビューフェーズ 🚀【新設】
        if (!context.HasFatalError())
        {
            // ナレッジを読み込む
            var knowledge = await _knowledgeBase.GetRelevantKnowledgeAsync(context);
            
            // AIにレビューを依頼
            var aiResponseJson = await _llmClient.ReviewWithKnowledgeAsync(context, knowledge);
            
            // AIの返答（JSON）を解析して、Issueリストに追加する
            ParseAndAddAiIssues(context, aiResponseJson);
        }

        // 3. エクスポートフェーズ
        if (!context.HasFatalError())
        {
            foreach (var exporter in _exporters)
            {
                exporter.Export("feature_auto", context.Nodes, outputDir);
            }
        }

        return context;
    }

    // 💡 AIのJSONを解析してコンテキストに追加するヘルパーメソッド
    private void ParseAndAddAiIssues(PipelineContext context, string jsonString)
    {
        if (string.IsNullOrWhiteSpace(jsonString)) return;

        try
        {
            // 大文字小文字を無視して柔軟にJSONを読み込む
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var aiIssues = JsonSerializer.Deserialize<List<AiIssueDto>>(jsonString, options);

            if (aiIssues != null)
            {
                foreach (var dto in aiIssues)
                {
                    // 文字列の "Warning" や "Error" を Enum に変換（失敗したらWarning）
                    var severity = Enum.TryParse<Severity>(dto.Severity, true, out var parsedSeverity)
                        ? parsedSeverity
                        : Severity.Warning;

                    // コンテキストにIssueを追加！
                    context.AddIssue(new RequirementIssue(
                        dto.RuleId ?? "AI-REVIEW-000",
                        dto.Message ?? "AIからの指摘事項があります。",
                        severity,
                        dto.NodeId ?? "Unknown"
                    ));
                }
            }
        }
        catch (JsonException ex)
        {
            // AIがJSON以外の謎のフォーマットを返してきた場合の安全網（フェールセーフ）
            context.AddIssue(new RequirementIssue("SYS-AI-ERR", $"AIの応答解析に失敗しました: {ex.Message}", Severity.Warning, "Pipeline"));
        }
    }

    // 💡 JSONをパースするためだけの一時的なクラス（DTO）
    private class AiIssueDto
    {
        public string? NodeId { get; set; }
        public string? RuleId { get; set; }
        public string? Message { get; set; }
        public string? Severity { get; set; }
    }
}