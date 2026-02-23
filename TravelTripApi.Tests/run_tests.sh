#!/bin/bash

# 🧪 TravelTrip API 測試執行腳本

echo "🚀 開始執行 TravelTrip API 測試套件..."
echo ""

# 移到測試目錄
cd "$(dirname "$0")"

# 清理之前的測試結果
echo "🧹 清理之前的測試結果..."
rm -rf TestResults/
rm -rf coveragereport/
echo ""

# 編譯專案
echo "🔨 編譯專案..."
dotnet build --configuration Debug --no-restore
if [ $? -ne 0 ]; then
    echo "❌ 編譯失敗"
    exit 1
fi
echo ""

# 執行所有測試並收集覆蓋率 (排除 Migrations)
echo "🧪 執行所有測試..."
dotnet test --no-build --no-restore --collect:"XPlat Code Coverage" \
    --logger "console;verbosity=normal" \
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Exclude="[*]*.Migrations.*"
TEST_RESULT=$?
echo ""

if [ $TEST_RESULT -eq 0 ]; then
    echo "✅ 所有測試通過！"
else
    echo "❌ 部分測試失敗"
fi
echo ""

# 生成覆蓋率報告
echo "📊 生成覆蓋率報告..."
if [ -d "TestResults" ]; then
    ~/.dotnet/tools/reportgenerator \
        -reports:"./TestResults/*/coverage.cobertura.xml" \
        -targetdir:"coveragereport" \
        -reporttypes:Html \
        -filefilters:-*Migrations*
    
    if [ $? -eq 0 ]; then
        echo "✅ 覆蓋率報告已生成"
        echo "📂 報告位置: coveragereport/index.html"
        echo ""
        echo "🌐 開啟報告:"
        echo "   open coveragereport/index.html"
        echo ""
    else
        echo "⚠️ 覆蓋率報告生成失敗"
    fi
else
    echo "⚠️ 找不到測試結果目錄"
fi

# 測試統計
echo "📈 測試統計:"
echo "   測試專案: TravelTripApi.Tests"
echo "   測試框架: xUnit"
echo "   覆蓋率工具: coverlet + reportgenerator"
echo ""

exit $TEST_RESULT
