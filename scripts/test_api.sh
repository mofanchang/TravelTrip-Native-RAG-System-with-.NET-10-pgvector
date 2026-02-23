#!/bin/bash

# TravelTrip API 測試腳本
# 用於測試所有 API 端點

BASE_URL="http://localhost:5000"
TOKEN=""

# 顏色輸出
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "=================================="
echo "TravelTrip API 完整測試"
echo "=================================="
echo ""

# 測試函數
test_endpoint() {
    local name=$1
    local method=$2
    local endpoint=$3
    local data=$4
    local auth=$5
    
    echo -e "${YELLOW}測試: ${name}${NC}"
    
    if [ "$auth" = "true" ] && [ -z "$TOKEN" ]; then
        echo -e "${RED}✗ 需要先登入${NC}"
        return 1
    fi
    
    local cmd="curl -s -X $method \"$BASE_URL$endpoint\""
    
    if [ -n "$data" ]; then
        cmd="$cmd -H 'Content-Type: application/json' -d '$data'"
    fi
    
    if [ "$auth" = "true" ]; then
        cmd="$cmd -H 'Authorization: Bearer $TOKEN'"
    fi
    
    response=$(eval $cmd)
    
    if echo "$response" | jq -e . >/dev/null 2>&1; then
        echo -e "${GREEN}✓ 成功${NC}"
        echo "$response" | jq '.' | head -20
        echo ""
        return 0
    else
        echo -e "${RED}✗ 失敗${NC}"
        echo "$response"
        echo ""
        return 1
    fi
}

# 1. 健康檢查
echo "=== 1. 健康檢查 ==="
test_endpoint "Trips Health" "GET" "/api/trips/health" "" false
test_endpoint "Accounts Health" "GET" "/api/accounts/health" "" false
test_endpoint "Bookings Health" "GET" "/api/bookings/health" "" false
test_endpoint "Cart Health" "GET" "/api/cart/health" "" false
test_endpoint "Chat Health" "GET" "/api/chat/health" "" false

# 2. 使用者註冊
echo "=== 2. 使用者認證 ==="
REGISTER_DATA='{
  "email": "test@example.com",
  "password": "test123456",
  "username": "testuser"
}'

echo -e "${YELLOW}註冊新使用者${NC}"
response=$(curl -s -X POST "$BASE_URL/api/accounts/register" \
  -H "Content-Type: application/json" \
  -d "$REGISTER_DATA")

if echo "$response" | jq -e '.token' >/dev/null 2>&1; then
    TOKEN=$(echo "$response" | jq -r '.token')
    echo -e "${GREEN}✓ 註冊成功，已取得 Token${NC}"
    echo "$response" | jq '.'
else
    echo -e "${YELLOW}註冊失敗（可能已存在），嘗試登入...${NC}"
    
    LOGIN_DATA='{
      "email": "test@example.com",
      "password": "test123456"
    }'
    
    response=$(curl -s -X POST "$BASE_URL/api/accounts/login" \
      -H "Content-Type: application/json" \
      -d "$LOGIN_DATA")
    
    TOKEN=$(echo "$response" | jq -r '.token')
    echo -e "${GREEN}✓ 登入成功${NC}"
    echo "$response" | jq '.'
fi
echo ""

# 3. 取得個人資料
test_endpoint "取得個人資料" "GET" "/api/accounts/profile" "" true

# 4. RAG 測試
echo "=== 3. RAG 向量搜尋測試 ==="
test_endpoint "測試 RAG 搜尋" "GET" "/api/chat/test-rag?query=日本&topK=3" "" false

# 5. 聊天機器人測試
echo "=== 4. AI 聊天機器人測試 ==="
CHAT_DATA='{
  "message": "我想去日本賞櫻",
  "topK": 3
}'
test_endpoint "AI 聊天" "POST" "/api/chat" "$CHAT_DATA" false

# 6. 購物車測試
echo "=== 5. 購物車功能測試 ==="
test_endpoint "查看購物車" "GET" "/api/cart" "" true

ADD_CART_DATA='{
  "tripId": 1,
  "quantity": 2
}'
test_endpoint "加入購物車" "POST" "/api/cart/add" "$ADD_CART_DATA" true

test_endpoint "再次查看購物車" "GET" "/api/cart" "" true

# 7. 訂單測試
echo "=== 6. 訂單功能測試 ==="

# 從購物車結帳
test_endpoint "購物車結帳" "POST" "/api/cart/checkout" "" true

test_endpoint "查看我的訂單" "GET" "/api/bookings/my-bookings" "" true

# 8. 直接建立訂單測試
BOOKING_DATA='{
  "items": [
    {
      "tripId": 1,
      "quantity": 1
    }
  ]
}'
test_endpoint "直接建立訂單" "POST" "/api/bookings" "$BOOKING_DATA" true

echo "=================================="
echo "測試完成！"
echo "=================================="

# 顯示 Token
if [ -n "$TOKEN" ]; then
    echo ""
    echo -e "${GREEN}JWT Token:${NC}"
    echo "$TOKEN"
    echo ""
    echo "使用此 Token 測試受保護的端點："
    echo "curl -H \"Authorization: Bearer $TOKEN\" $BASE_URL/api/accounts/profile"
fi
