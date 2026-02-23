# TravelTrip 系統架構圖 (Architecture Diagram)

本專案採用分散式架構，將耗時的 AI 向量化處理移至背景服務，確保 API 的高可用性與回應速度。

## 系統架構概覽

```mermaid
graph TD
    subgraph Client_Layer [用戶端層]
        FE["React / Vite Frontend<br/>(Port 3000)"]
    end

    subgraph API_Layer [API 服務層]
        API["ASP.NET Core API<br/>(Port 5000)"]
        Service["TripRagService / AuthService<br/>(Business Logic)"]
        API --> Service
    end

    subgraph Worker_Layer [背景處理層]
        Worker["TravelTrip.Worker<br/>(Background Service)"]
        AI["Local Text Embedder<br/>(SmartComponents)"]
        Worker --> AI
    end

    subgraph Data_Layer [資料持久層]
        DB[("PostgreSQL + pgvector<br/>(Port 5432)")]
    end

    %% 交互關係
    FE -- "HTTPS / JWT" --> API
    Service -- "EF Core 10 / Npgsql" --> DB
    Worker -- "EF Core 10 / Search Un-vectorized" --> DB
    Worker -- "Update Embedding Vector" --> DB
    Service -- "Vector Search / RAG" --> DB
```

## 核心組件說明

### 1. Frontend (React / Vite)
- 提供使用者介面，透過 JWT 與 API 進行驗證。
- 呼叫 `/api/trips/search` 進行 AI 智慧搜尋。

### 2. TravelTripApi (.NET 10)
- **職責**：處理 CRUD、身份驗證、以及即時搜尋請求。
- **RAG 實作**：使用 `TripRagService` 調用 `pgvector` 進行語義搜尋。
- **效能優化**：不直接負責向量生成，避免阻塞 HTTP 請求。

### 3. TravelTrip.Worker (Background Service)
- **職責**：非同步處理 AI 向量化。
- **流程**：
    1. 每 30 秒掃描資料庫中尚未向量化的行程（`Embedding == null`）。
    2. 使用 `LocalTextEmbedder`（SmartComponents）生成向量。
    3. 更新資料庫中的向量欄位。

### 4. PostgreSQL + pgvector
- **儲存**：所有的行程資料與使用者資訊。
- **向量搜尋**：支援原生 `<->` (L2 Distance) 或 `<#>` (Inner Product) 運算符，實作高效的 RAG 查詢。

## 關鍵流程：非同步向量化 (RAG Flow)

```mermaid
sequenceDiagram
    participant User as 使用者/管理員
    participant API as TravelTrip API
    participant DB as PostgreSQL
    participant Worker as Background Worker

    User->>API: 建立新行程 (POST /api/trips)
    API->>DB: 儲存行程資料 (Embedding 為空)
    API-->>User: 回傳成功 (201 Created)
    
    Note over Worker, DB: 背景輪詢程序
    Worker->>DB: 查詢待向量化行程
    DB-->>Worker: 回傳行程列表
    Worker->>Worker: 產生向量 (Local AI)
    Worker->>DB: 更新向量欄位
    
    User->>API: 進行語義搜尋
    API->>DB: 執行向量搜尋 (pgvector)
    DB-->>API: 回傳相似行程
    API-->>User: 展示 RAG 搜尋結果
```
