using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReqPipeline.Core.Models;

// AIが返してくるJSONのスキーマ定義
public class QaPerspectiveResult
{
    [JsonPropertyName("perspectives")] 
    public List<QaPerspective> Perspectives { get; set; } = new();
}

public class QaPerspective
{
    [JsonPropertyName("category")] 
    public string Category { get; set; } = ""; // 例: 状態遷移, 境界値, 異常系, タイミング

    [JsonPropertyName("viewpoint")] 
    public string Viewpoint { get; set; } = ""; // テスト観点（何を検証するか）

    [JsonPropertyName("description")] 
    public string Description { get; set; } = ""; // なぜその観点が必要か、QAからの問いかけ
}