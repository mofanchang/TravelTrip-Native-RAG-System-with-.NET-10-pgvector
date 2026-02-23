#!/bin/bash

echo "🧹 清理並重新編譯..."
cd /Users/Flora/Documents/TRAVELTRIP_NET_VERSION/TravelTripApi.Tests

# 清理
dotnet clean > /dev/null 2>&1

# 編譯
echo "🔨 編譯專案..."
dotnet build --configuration Debug

if [ $? -eq 0 ]; then
    echo "✅ 編譯成功！"
    echo ""
    echo "🧪 執行測試..."
    dotnet test --no-build --logger "console;verbosity=normal"
else
    echo "❌ 編譯失敗，請檢查錯誤訊息"
    exit 1
fi
