using System.Threading.Tasks;
using ReqPipeline.Core.Models;


namespace ReqPipeline.Core.Interfaces;

// 特定のLLM（OllamaやOpenAI）に依存しない、純粋なテキスト生成インターフェース
public interface ILlmClient
{
    Task<string> ReviewWithKnowledgeAsync(PipelineContext context, string knowledge);
    Task<string> GenerateTextAsync(string prompt, float temperature = 0.0f);
}