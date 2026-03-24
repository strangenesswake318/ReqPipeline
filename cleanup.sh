#!/bin/bash

echo "🔄 ブランチ切り替え後のクリーンアップを開始します..."

# 1. 起動中のWebアプリ（dotnet run / watch）だけを狙い撃ちして停止
# ※VS Codeの拡張機能（Roslyn等）は生かしておくのがポイントです！
echo "🛑 起動中のアプリを停止しています..."
pkill -f "dotnet run" || true
pkill -f "dotnet watch" || true
pkill -f "ReqPipeline.Web" || true

# 2. 古いビルドのゴミ (bin/obj) を完全消去
echo "🧹 古いビルドファイルを削除しています..."
dotnet clean

# 3. 新しいブランチの構成に合わせてパッケージを復元
echo "📦 NuGetパッケージを復元しています..."
dotnet restore

# 4. 試しにビルドして、構造が壊れていないかチェック
echo "🔨 プロジェクトをビルドしています..."
dotnet build

echo "✨ クリーンアップ完了！"
echo "💡 もしVS Codeのコード補完がおかしい場合は『Ctrl+Shift+P』→『Reload Window』を実行してください。"