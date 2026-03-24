using System.Threading.Tasks;
using ReqPipeline.Core.Models;

namespace ReqPipeline.Core.Interfaces;

public interface IRequirementStaticAnalysis
{
    Task ValidateAsync(PipelineContext context);
}