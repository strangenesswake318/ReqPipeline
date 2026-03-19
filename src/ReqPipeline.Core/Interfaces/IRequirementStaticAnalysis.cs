using System.Threading.Tasks;
using ReqPipeline.Core.Models;

namespace ReqPipeline.Core.Interfaces;

// すべてのバリデーター（LinterもAIも）が実装すべき共通インターフェース
public interface IRequirementStaticAnalysis
{
    // Contextを受け取り、検証結果をContextの中のIssuesに追加していく
    Task ValidateAsync(PipelineContext context);
}