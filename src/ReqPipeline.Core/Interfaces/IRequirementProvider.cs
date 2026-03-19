using System.Collections.Generic;
using ReqPipeline.Core.Models;

namespace ReqPipeline.Core.Interfaces;

public interface IRequirementProvider
{
    IEnumerable<RequirementNode> Load(string sourcePath);

    void Save(IEnumerable<RequirementNode> nodes, string path);
}