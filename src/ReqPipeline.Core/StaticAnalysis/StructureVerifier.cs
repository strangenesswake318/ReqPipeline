using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReqPipeline.Core.Interfaces;
using ReqPipeline.Core.Models;

namespace ReqPipeline.Core.StaticAnalysis;

public class StructureVerifier : IRequirementStaticAnalysis
{
    public Task ValidateAsync(PipelineContext context)
    {
        var nodes = context.Nodes.ToList();
        var nodeDictionary = nodes.ToDictionary(n => n.Id);

        foreach (var node in nodes)
        {
            // 1. 親ノードの存在チェック（ルート要求以外は親が必須）
            if (node.ParentId == null)
            {
                if (node.Type != UsdmType.Requirement)
                {
                    context.AddIssue(new RequirementIssue("STR001", $"[{node.Type}] '{node.Description}' はルートノードになれません。親IDが必要です。", Severity.Error));
                }
                continue; // ルート要求の場合はこれ以降の親チェックは不要
            }

            // 親IDが実際にデータ内に存在するかチェック（孤立ノードの防止）
            if (!nodeDictionary.TryGetValue(node.ParentId.Value, out var parent))
            {
                context.AddIssue(new RequirementIssue("STR002", $"[{node.Type}] '{node.Description}' の親ノード({node.ParentId})が存在しません。", Severity.Error));
                continue;
            }

            // 2. USDMの階層（ネスト）ルールチェック
            switch (node.Type)
            {
                case UsdmType.Requirement:
                    // 【修正点】要求の親は「要求」のみ許可（ネストの許容）
                    if (parent.Type != UsdmType.Requirement)
                    {
                        context.AddIssue(new RequirementIssue("STR003", $"要求 '{node.Description}' の親は「要求」である必要がありますが、[{parent.Type}] になっています。", Severity.Error));
                    }
                    break;

                case UsdmType.Rationale:
                    // 理由の親は「要求」のみ許可
                    if (parent.Type != UsdmType.Requirement)
                    {
                        context.AddIssue(new RequirementIssue("STR004", $"理由 '{node.Description}' の親は「要求」である必要がありますが、[{parent.Type}] になっています。", Severity.Error));
                    }
                    break;

                case UsdmType.Specification:
                    // 仕様の親は「理由」のみ許可
                    if (parent.Type != UsdmType.Rationale)
                    {
                        context.AddIssue(new RequirementIssue("STR005", $"仕様 '{node.Description}' の親は「理由」である必要がありますが、[{parent.Type}] になっています。", Severity.Error));
                    }
                    
                    // 3. EARSコンテキストの必須チェック
                    if (node.EarsContext == null)
                    {
                        context.AddIssue(new RequirementIssue("STR006", $"仕様 '{node.Description}' に EARS (トリガーやアクター等の振る舞い定義) が設定されていません。", Severity.Error));
                    }
                    break;
            }
        }

        return Task.CompletedTask;
    }
}