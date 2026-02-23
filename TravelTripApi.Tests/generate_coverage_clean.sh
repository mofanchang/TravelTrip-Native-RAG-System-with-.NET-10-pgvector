#!/bin/bash

# 清理 NuGet 快取
echo "🧹 清理 NuGet 快取..."
dotnet nuget locals all --clear

# 等待一下
sleep 2

# 設置 PATH
export PATH="$PATH:/Users/Flora/.dotnet/tools"

# 清理之前的報告
rm -rf TestResults coveragereport

# 執行測試並收集覆蓋率（使用 --no-restore 避免鎖定問題）
echo "🧪 執行測試並收集覆蓋率..."
dotnet build --no-restore
dotnet test --no-build --collect:"XPlat Code Coverage" --logger "console;verbosity=normal"

# 生成 HTML 報告
echo "📊 生成覆蓋率報告..."
reportgenerator \
  -reports:"./TestResults/*/coverage.cobertura.xml" \
  -targetdir:"coveragereport" \
  -reporttypes:Html

# 打開報告
echo "✅ 完成！正在打開報告..."
open coveragereport/index.html

echo ""
echo "📁 報告位置: $(pwd)/coveragereport/index.html"
