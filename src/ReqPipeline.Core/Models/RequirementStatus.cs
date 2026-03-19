namespace ReqPipeline.Core.Models;

public enum RequirementStatus
{
    Draft,          // 編集直後
    LinterOk,       // EARS/USDM構文チェック合格
    Staged,         // ステージング済み（AI検証待ち）
    Validating,     // AI検証中
    Validated,      // AI検証合格
    ReviewRequired, // AIまたは人間による指摘あり
    Approved        // 最終承認済み（CC-SDD出力対象）
}