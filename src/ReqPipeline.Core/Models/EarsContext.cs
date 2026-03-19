namespace ReqPipeline.Core.Models;

public class EarsContext
{
    // すべて init から set に変更
    public string Pattern { get; set; } = "EventDriven";
    public string Trigger { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
}