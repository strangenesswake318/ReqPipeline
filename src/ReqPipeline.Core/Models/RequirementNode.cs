namespace ReqPipeline.Core.Models;

public class RequirementNode
{
    // Guid型に統一し、set 可能にする
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ParentId { get; set; }
    public UsdmType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public EarsContext? EarsContext { get; set; }

    // パラメーターなしコンストラクター（重要：Blazorやシリアライザ用）
    public RequirementNode() { }

    // 既存のコードが依存している場合のコンストラクター
    public RequirementNode(Guid id, Guid? parentId, UsdmType type, string description)
    {
        Id = id;
        ParentId = parentId;
        Type = type;
        Description = description;
    }
}