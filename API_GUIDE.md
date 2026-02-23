# TravelTrip .NET API 完整指南

## 📚 目錄
1. [快速開始](#快速開始)
2. [系統架構](#系統架構)
3. [API 端點](#api-端點)
4. [資料庫設定](#資料庫設定)
5. [部署指南](#部署指南)

---

## 🚀 快速開始

### 前置需求
- .NET 8.0 SDK
- PostgreSQL 15+ with pgvector extension
- (選用) OpenAI API Key for real embeddings

### 安裝步驟

```bash
# 1. Clone 專案
cd /Users/Flora/Documents/TRAVELTRIP_NET_VERSION/TravelTripApi

# 2. 還原套件
dotnet restore

# 3. 設定資料庫連線
# 編輯 appsettings.json 設定 ConnectionStrings:DefaultConnection

# 4. 建置專案
dotnet build

# 5. 執行專案
dotnet run

# 6. 瀏覽 Swagger 文件
# 開啟瀏覽器：https://localhost:5001/swagger
```

---

## 🏗️ 系統架構

```
TravelTripApi/
├── Controllers/          # API 端點
│   ├── AccountsController.cs
│   ├── BookingsController.cs
│   ├── CartController.cs
│   ├── ChatController.cs
│   └── TripsController.cs
├── Services/            # 業務邏輯層
│   ├── AuthService.cs
│   ├── BookingService.cs
│   ├── CartService.cs
│   ├── ChatbotService.cs
│   └── TripRagService.cs
├── Models/              # 資料模型
│   ├── UserEntity.cs
│   ├── TripEntity.cs
│   ├── BookingEntity.cs
│   ├── CartEntity.cs
│   └── ChatHistoryEntity.cs
├── DTOs/                # 資料傳輸物件
├── Data/                # 資料庫上下文
│   └── TravelDbContext.cs
└── Program.cs           # 應用程式進入點
```

### 技術棧
- **Framework**: ASP.NET Core 8.0
- **Database**: PostgreSQL 15+ with pgvector
- **ORM**: Entity Framework Core 8.0
- **Authentication**: JWT Bearer Token
- **AI/ML**: Microsoft Semantic Kernel
- **Vector Search**: pgvector (Cosine Distance)

---

## 📡 API 端點

### 🔐 認證 (Accounts)

#### 註冊
```http
POST /api/accounts/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123",
  "username": "testuser"
}
```

#### 登入
```http
POST /api/accounts/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}
```

**回應範例：**
```json
{
  "success": true,
  "message": "登入成功",
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "user": {
    "id": 1,
    "email": "user@example.com",
    "username": "testuser"
  }
}
```

#### 取得個人資料
```http
GET /api/accounts/profile
Authorization: Bearer {token}
```

---

### 🗺️ 行程 (Trips)

#### 搜尋相似行程（RAG）
```http
GET /api/trips/search?query=想去日本賞櫻&topK=5
Authorization: Bearer {token} (optional)
```

**回應範例：**
```json
{
  "query": "想去日本賞櫻",
  "count": 1,
  "results": [
    {
      "id": 1,
      "name": "京都賞櫻之旅",
      "country": "日本",
      "city": "京都",
      "duration": 5,
      "price": 32000,
      "similarityScore": 0.85
    }
  ]
}
```

#### 初始化 RAG 系統
```http
POST /api/trips/initialize-rag
```

---

### 🛒 購物車 (Cart)

#### 取得購物車
```http
GET /api/cart
Authorization: Bearer {token}
```

#### 加入商品
```http
POST /api/cart/add
Authorization: Bearer {token}
Content-Type: application/json

{
  "tripId": 1,
  "quantity": 2
}
```

#### 更新數量
```http
PUT /api/cart/items/{itemId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "quantity": 3
}
```

#### 移除項目
```http
DELETE /api/cart/items/{itemId}
Authorization: Bearer {token}
```

#### 清空購物車
```http
DELETE /api/cart/clear
Authorization: Bearer {token}
```

#### 購物車結帳
```http
POST /api/cart/checkout
Authorization: Bearer {token}
```

---

### 📦 訂單 (Bookings)

#### 建立訂單
```http
POST /api/bookings
Authorization: Bearer {token}
Content-Type: application/json

{
  "items": [
    {
      "tripId": 1,
      "quantity": 2
    }
  ]
}
```

#### 取得訂單列表
```http
GET /api/bookings/my-bookings?skip=0&take=20
Authorization: Bearer {token}
```

#### 取得單一訂單
```http
GET /api/bookings/{id}
Authorization: Bearer {token}
```

#### 更新訂單狀態
```http
PUT /api/bookings/{id}/status
Authorization: Bearer {token}
Content-Type: application/json

{
  "status": "Completed",
  "paypalTransactionId": "PAYPAL-123456"
}
```

#### 取消訂單
```http
POST /api/bookings/{id}/cancel
Authorization: Bearer {token}
```

---

### 💬 AI 聊天 (Chat)

#### 聊天（主要端點）
```http
POST /api/chat
Content-Type: application/json

{
  "message": "推薦適合家庭旅遊的行程",
  "sessionId": "optional-session-id",
  "topK": 3
}
```

**回應範例：**
```json
{
  "query": "推薦適合家庭旅遊的行程",
  "aiResponse": "太好了！我找到了非常符合您需求的行程...",
  "recommendedTrips": [
    {
      "id": 1,
      "name": "京都賞櫻之旅",
      "similarityScore": 0.85,
      "matchType": "RAG_HIGH_QUALITY"
    }
  ],
  "total": 1,
  "method": "RAG",
  "sessionId": "generated-session-id"
}
```

#### 簡易推薦
```http
GET /api/chat/recommend?query=想去歐洲
```

#### 取得聊天歷史
```http
GET /api/chat/history/{sessionId}
```

#### 測試 RAG 搜尋
```http
GET /api/chat/test-rag?query=日本&topK=3
```

---

## 🗄️ 資料庫設定

### PostgreSQL 配置

```sql
-- 1. 建立資料庫
CREATE DATABASE traveltrip_db;

-- 2. 啟用 pgvector 擴展
\c traveltrip_db
CREATE EXTENSION IF NOT EXISTS vector;

-- 3. 驗證擴展
SELECT * FROM pg_extension WHERE extname = 'vector';
```

### 連線字串配置

編輯 `appsettings.json`：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=traveltrip_db;Username=postgres;Password=your_password"
  }
}
```

### 資料庫遷移

```bash
# 安裝 EF Core Tools
dotnet tool install --global dotnet-ef

# 建立遷移
dotnet ef migrations add InitialCreate

# 更新資料庫
dotnet ef database update
```

---

## 🚀 部署指南

### Docker 部署

建立 `Dockerfile`：

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["TravelTripApi.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TravelTripApi.dll"]
```

建立 `docker-compose.yml`：

```yaml
version: '3.8'
services:
  api:
    build: .
    ports:
      - "5000:80"
    environment:
      - ConnectionStrings__DefaultConnection=Host=db;Database=traveltrip_db;Username=postgres;Password=postgres
    depends_on:
      - db

  db:
    image: pgvector/pgvector:pg15
    environment:
      - POSTGRES_DB=traveltrip_db
      - POSTGRES_PASSWORD=postgres
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
```

執行：
```bash
docker-compose up -d
```

---

## 🔧 設定 OpenAI Embedding

要使用真實的 AI Embedding，取消 `Program.cs` 中的註解：

```csharp
// 移除 Mock Service
// builder.Services.AddSingleton<ITextEmbeddingGenerationService>(new MockEmbeddingService());

// 加入 OpenAI
builder.Services.AddSingleton<ITextEmbeddingGenerationService>(sp =>
{
    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.AddOpenAITextEmbeddingGeneration(
        modelId: "text-embedding-3-small",
        apiKey: builder.Configuration["OpenAI:ApiKey"]
    );
    var kernel = kernelBuilder.Build();
    return kernel.GetRequiredService<ITextEmbeddingGenerationService>();
});
```

在 `appsettings.json` 設定 API Key：

```json
{
  "OpenAI": {
    "ApiKey": "sk-your-openai-api-key"
  }
}
```

---

## 📊 效能優化

### 向量搜尋索引

```sql
-- 建立 HNSW 索引加速向量搜尋
CREATE INDEX ON trips_trip 
USING hnsw (embedding vector_cosine_ops);
```

### 連線池設定

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=traveltrip_db;Username=postgres;Password=pwd;Pooling=true;MinPoolSize=5;MaxPoolSize=100"
  }
}
```

---

## 🔒 安全性建議

1. **JWT Secret**：使用強密碼（至少 32 字元）
2. **HTTPS**：生產環境必須使用 HTTPS
3. **CORS**：限制允許的來源網域
4. **Rate Limiting**：加入 API 請求頻率限制
5. **密碼政策**：強制使用強密碼

---

## 📝 授權

MIT License

---

## 👥 貢獻

歡迎提交 Issue 和 Pull Request！
