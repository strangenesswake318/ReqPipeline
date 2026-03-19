# 次世代要求定義パイプライン：プロセス定義 v1.0

## 1. 概要

本プロジェクトでは、USDM（Universal Specification Describing Manner）と EARS（Easy Approach to Requirements Syntax）を統合し、機械的 Linter と AI（LLM）による意味論検証を組み合わせた要求定義プロセスを採用する。

## 2. 要求のライフサイクル（ステータス遷移）

要求（Requirement/Rationale/Specification）は、以下のプロセスを経て「承認済み」へと昇華される。

| ステータス | 定義 | 遷移条件（トリガー） |
| --- | --- | --- |
| **Draft** | 記述中・未完成 | ユーザーによる新規作成・編集 |
| **LinterOk** | 構文的に正しい | 機械的 Linter（C#）が EARS 構文・禁止表現をパス |
| **Staged** | 検証待ち | ユーザーによる「ステージング」操作 |
| **Validating** | AI 検証中 | バックグラウンドサービスが AI API へ送信 |
| **Validated** | 意味論的に妥当 | AI が論理矛盾なしと判定 |
| **ReviewRequired** | 指摘あり | Linter または AI が不備を検知し、ユーザーに通知 |
| **Approved** | 最終承認済み | ユーザーが内容を確定。後段の CC-SDD へ出力可能 |

---

## 3. 品質ゲート（検証ルール）

### A. 機械的検証 (Mechanical Linter)

即時フィードバックを行うルール。

* **EARS 構文遵守**: `When... the System shall...` 等のパターンに適合しているか。
* **主語の必須化**: 「[システム]は～」など、責務の所在が明確か。
* **禁止表現の検知**: 否定表現（～しない）、二重否定、曖昧語（～等、～など）の排除。

### B. 意味論検証 (AI Semantic Validation)

ステージング後に実行される深い検証。

* **USDM 整合性**: 「要求」に対する「理由」が論理的か。「理由」を解決する「仕様」に飛躍がないか。
* **用語集（Glossary）チェック**: プロジェクト定義の用語が正しく使われているか。
* **コンフリクト検出**: 他の `RequirementNode` との機能的矛盾がないか。

---

## 4. アウトプット（成果物）

承認された要求は、以下の形式で自動生成される。

1. **CC-SDD 用 Markdown**: `.kiro/specs/{feature}/requirements.md`
2. **ステークホルダー用 Excel**: 進捗管理・合意形成用
3. **グラフィカル要求図**: 構造の俯瞰用

---