using System.Linq;
using System.Threading.Tasks;
using ReqPipeline.Core.Interfaces;
using ReqPipeline.Core.Models;

namespace ReqPipeline.Core.StaticAnalysis;

public class StructureVerifier : IRequirementStaticAnalysis
{
    // 💡 インターフェースの約束通り ValidateAsync に変更
    public virtual Task ValidateAsync(PipelineContext context)
    {
        var nodes = context.Nodes.ToList();
        var nodeDictionary = nodes.ToDictionary(n => n.Id);

        foreach (var node in nodes)
        {
            // 1. ルートノード（親なし）のチェック
            if (node.ParentId == null)
            {
                if (node.Type != UsdmType.ParentRequirement)
                {
                    // 💡 戻り値ではなく、context.AddIssue で直接バケツにエラーを入れる
                    // 💡 引数の順番も (RuleId, Message, Severity) に統一
                    context.AddIssue(new RequirementIssue(
                        "STR001", 
                        $"[{node.Type}] '{node.Description}' はルートノードになれません。親IDが必要です。", 
                        Severity.Error));
                }
                continue; 
            }

            // 孤立ノードの防止
            if (!nodeDictionary.TryGetValue(node.ParentId.Value, out var parent))
            {
                context.AddIssue(new RequirementIssue(
                    "STR002", 
                    $"[{node.Type}] '{node.Description}' の親ノード({node.ParentId})が存在しません。", 
                    Severity.Error));
                continue;
            }

            // 2. 4層アーキテクチャの階層（ネスト）ルールチェック
            switch (node.Type)
            {
                case UsdmType.ParentRequirement:
                    context.AddIssue(new RequirementIssue(
                        "STR003", 
                        $"親要求(Epic) '{node.Description}' は最上位でなければなりません。親を持たせないでください。", 
                        Severity.Error));
                    break;

                case UsdmType.Rationale:
                    if (parent.Type != UsdmType.ParentRequirement)
                        context.AddIssue(new RequirementIssue(
                            "STR004", 
                            $"理由 '{node.Description}' の親は「親要求」である必要がありますが、[{parent.Type}] になっています。", 
                            Severity.Error));
                    break;

                case UsdmType.ChildRequirement:
                    if (parent.Type != UsdmType.Rationale)
                        context.AddIssue(new RequirementIssue(
                            "STR005", 
                            $"子要求 '{node.Description}' の親は「理由」である必要がありますが、[{parent.Type}] になっています。", 
                            Severity.Error));
                    
                    if (node.GherkinContext == null)
                        context.AddIssue(new RequirementIssue(
                            "STR006", 
                            $"子要求 '{node.Description}' に BDD (Gherkin: Given/When/Then) の定義が設定されていません。", 
                            Severity.Error));
                    break;

                case UsdmType.Specification:
                    if (parent.Type != UsdmType.ChildRequirement)
                        context.AddIssue(new RequirementIssue(
                            "STR007", 
                            $"仕様 '{node.Description}' の親は「子要求」である必要がありますが、[{parent.Type}] になっています。", 
                            Severity.Error));
                    
                    if (node.EarsContext == null)
                        context.AddIssue(new RequirementIssue(
                            "STR008", 
                            $"仕様 '{node.Description}' に EARS (トリガーやアクター等の振る舞い定義) が設定されていません。", 
                            Severity.Error));
                    break;
            }
        }

        // 💡 同期処理なので Task.CompletedTask を返す
        return Task.CompletedTask;
    }
}