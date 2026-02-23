#!/bin/bash

# 確保 dotnet tools 在 PATH 中
export PATH="$PATH:/Users/Flora/.dotnet/tools"

# 清理之前的報告
rm -rf TestResults coveragereport

# 執行測試並收集覆蓋率 (排除 Migrations)
echo "🧪 執行測試並收集覆蓋率..."
dotnet test --collect:"XPlat Code Coverage" \
  --logger "console;verbosity=normal" \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Exclude="[*]*.Migrations.*"

# 生成 HTML 報告
echo "📊 生成覆蓋率報告..."
reportgenerator \
  -reports:"./TestResults/*/coverage.cobertura.xml" \
  -targetdir:"coveragereport" \
  -reporttypes:Html \
  -filefilters:-*Migrations*

# 打開報告
echo "✅ 完成！正在打開報告..."
open coveragereport/index.html

echo ""
echo "📁 報告位置: $(pwd)/coveragereport/index.html"
