namespace ReqPipeline.Core.Models;

// 💡 状態遷移やイベントを表現しやすくするため、State と Event を追加するとDDD的に強力です
public enum GlossaryCategory { Actor, Action, Object, State, Event }

// UIから編集（@bind）できるように、record から get; set; を持つ class に変更します
public class GlossaryEntry
{
    // UIでの編集・削除を特定するためのID
    public Guid Id { get; set; } = Guid.NewGuid();

    // 💡 新規追加：ネームスペース（スコープ）
    public string Namespace { get; set; } = "SYS";

    public string Term { get; set; } = string.Empty;
    public GlossaryCategory Category { get; set; } = GlossaryCategory.Object;
    public string Definition { get; set; } = string.Empty;
    public List<string>? AllowedEarsFields { get; set; }

    // LinterやLLMで使う時の「完全修飾名」（例: "[SYS:Emergency_Event]"）
    public string FullName => $"[{Namespace}:{Term}]";
}

public class Glossary
{
    public string Project { get; set; } = "DefaultProject";
    public List<GlossaryEntry> Entries { get; set; } = new();

    // ネームスペースも考慮した検索メソッドにアップグレードすることも可能です
    public bool Contains(string term) => Entries.Any(e => e.Term == term);
    public GlossaryEntry? GetEntry(string term) => Entries.FirstOrDefault(e => e.Term == term);
}