using System;
using System.Threading.Tasks;
using ReqPipeline.Core.Interfaces;
using ReqPipeline.Core.Models;
using ReqPipeline.Core.StaticAnalysis;


namespace ReqPipeline.Core.Application; // ※ご自身のnamespaceに合わせてください

public class PipelineOrchestrator
{
    private readonly StructureVerifier _structureVerifier;
    private readonly GlossaryVerifier _glossaryVerifier;
    private readonly IRequirementStaticAnalysis _semanticValidator;

    // コンストラクタで、必要な演奏者（Validator）を全員集める
    public PipelineOrchestrator(
        StructureVerifier structureVerifier,
        GlossaryVerifier glossaryVerifier,
        IRequirementStaticAnalysis semanticValidator)
    {
        _structureVerifier = structureVerifier;
        _glossaryVerifier = glossaryVerifier;
        _semanticValidator = semanticValidator;
    }

    public async Task RunPipelineAsync(PipelineContext context)
    {
        Console.WriteLine("\n======================================");
        Console.WriteLine("🚀 [Pipeline] 要求仕様検証パイプライン 開始");
        Console.WriteLine("======================================");

        // --------------------------------------------------
        // 第1楽章：構造検証（StructureVerifier）
        // --------------------------------------------------
        Console.WriteLine("🔍 [Pipeline] 1. 構造検証 実行中...");
        await _structureVerifier.ValidateAsync(context);
        
        if (context.HasFatalError()) 
        {
            Console.WriteLine("❌ [Pipeline] 構造検証で致命的なエラーが発生したため、パイプラインを停止します。");
            return;
        }

        // --------------------------------------------------
        // 第2楽章：用語集検証（GlossaryVerifier）
        // --------------------------------------------------
        Console.WriteLine("🔍 [Pipeline] 2. 用語集検証 実行中...");
        await _glossaryVerifier.ValidateAsync(context);
        
        if (context.HasFatalError()) 
        {
            Console.WriteLine("❌ [Pipeline] 用語集検証で致命的なエラーが発生したため、パイプラインを停止します。");
            return;
        }

        // --------------------------------------------------
        // 第3楽章：統合AIレビュー（SemanticValidator）
        // --------------------------------------------------
        Console.WriteLine("🧠 [Pipeline] 3. 統合AIレビュー 実行中...");
        
        // 💡 実働とログ出しはすべて SemanticValidator に委譲する！
        await _semanticValidator.ValidateAsync(context);

        // --------------------------------------------------
        // パイプライン完了
        // --------------------------------------------------
        Console.WriteLine("======================================");
        Console.WriteLine("✅ [Pipeline] すべての検証パイプラインが完了しました！");
        Console.WriteLine("======================================\n");
    }
}