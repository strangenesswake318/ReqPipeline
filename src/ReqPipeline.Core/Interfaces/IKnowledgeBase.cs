using ReqPipeline.Core.Models;

namespace ReqPipeline.Core.Interfaces;

/// <summary>
/// 山本メソッド等の要求工学ナレッジを提供するインターフェース
/// </summary>
public interface IKnowledgeBase
{
    /// <summary>
    /// コンテキスト（現在の要求仕様や用語）に基づいて、関連する知見を抽出する
    /// </summary>
    Task<string> GetRelevantKnowledgeAsync(PipelineContext context);
}