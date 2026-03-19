using ReqPipeline.Core.Models;

namespace ReqPipeline.Core.Interfaces;

public interface IGlossaryProvider
{
    Glossary Load(string sourcePath);
    void Save(Glossary glossary, string path);
}