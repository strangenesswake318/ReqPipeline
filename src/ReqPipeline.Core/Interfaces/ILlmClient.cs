using System.Threading.Tasks;

namespace ReqPipeline.Core.Interfaces;

// 特定のLLM（OllamaやOpenAI）に依存しない、純粋なテキスト生成インターフェース
public interface ILlmClient
{
    Task<string> GenerateTextAsync(string prompt, float temperature = 0.0f);
}