using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ReqPipeline.Core.Interfaces;
using ReqPipeline.Core.Models;

namespace ReqPipeline.Core.StaticAnalysis;

// 💡 古い IRequirementRule ではなく、統一インターフェースの IRequirementStaticAnalysis を実装
public class GlossaryVerifier : IRequirementStaticAnalysis
{
    // 💡戻り値でListを返すのではなく、Taskを返しつつ context に Issue を直接追加する設計
    public Task ValidateAsync(PipelineContext context)
    {
        var glossary = context.Glossary;

        // 仕様（Specification）を対象とする
        var specs = context.Nodes.Where(n => n.Type == UsdmType.Specification);

        foreach (var spec in specs)
        {
            // EARSの要素を含めたチェック用テキストを生成
            var textToCheck = spec.Description;
            if (spec.EarsContext != null)
            {
                textToCheck += $" {spec.EarsContext.Trigger} {spec.EarsContext.Actor} {spec.EarsContext.Response}";
            }

            // ==========================================
            // 1. ネームスペース（ドメイン境界）のチェック
            // ==========================================
            var matches = Regex.Matches(textToCheck, @"\[(.*?)\]");
            foreach (Match match in matches)
            {
                var fullTag = match.Value; // 例: "[SYS:Emergency_Event]"
                
                // GlossaryEntry の FullName プロパティと一致するか
                var isValidTerm = glossary.Entries.Any(e => e.FullName == fullTag);

                if (!isValidTerm)
                {
                    // 💡 リストに追加して返すのではなく、ContextのAddIssueを呼ぶ
                    context.AddIssue(new RequirementIssue(
                        "GLOS-002",
                        $"未定義の用語スコープ: {fullTag} が使われています。用語集(Glossary)で定義された正しいネームスペースと用語を使用してください。\n該当箇所: {spec.Description}",
                        Severity.Error
                    ));
                }
            }

            // ==========================================
            // 2. 曖昧な否定表現のチェック
            // ==========================================
            var negativePattern = @"ないこと|しない";
            var hasNegativeExpression = Regex.IsMatch(spec.Description, negativePattern) || 
                                        (spec.EarsContext != null && Regex.IsMatch(spec.EarsContext.Response, negativePattern));

            if (hasNegativeExpression)
            {
                // 💡 こちらも Context に直接追加
                context.AddIssue(new RequirementIssue(
                    "GLOS-003",
                    $"検証不可能な否定表現が含まれています。「～しないこと」ではなく、システムが「どう振る舞うべきか（代替アクション・安全状態への遷移など）」を肯定形で明記してください。\n該当箇所: {spec.Description}",
                    Severity.Warning
                ));
            }
        }

        // 非同期インターフェースを満たすため、完了済みのタスクを返す
        return Task.CompletedTask;
    }
}