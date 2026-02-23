#!/bin/bash

echo "🔨 開始編譯 TravelTripApi..."
cd /Users/Flora/Documents/TRAVELTRIP_NET_VERSION
dotnet build TravelTripApi/TravelTripApi.csproj

if [ $? -eq 0 ]; then
    echo "✅ 編譯成功！"
else
    echo "❌ 編譯失敗"
    exit 1
fi
