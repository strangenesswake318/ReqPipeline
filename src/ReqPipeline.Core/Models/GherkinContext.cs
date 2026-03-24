namespace ReqPipeline.Core.Models;

/// <summary>
/// BDD (振る舞い駆動開発) のためのマクロ検証コンテキスト
/// </summary>
public class GherkinContext
{
    // [SYS:...] や [SW:...] などの階層化Glossaryタグを用いて状態を記述する
    public string Given { get; set; } = string.Empty; // 前提状態
    public string When { get; set; } = string.Empty;  // トリガー（1つの動詞に限定）
    public string Then { get; set; } = string.Empty;  // 結果状態
}