# TravelTrip: Native RAG Travel Assistant
[![.NET 10](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/Frontend-React-61dafb.svg)](https://reactjs.org/)
[![PostgreSQL](https://img.shields.io/badge/Database-PostgreSQL-336791.svg)](https://www.postgresql.org/)

[繁體中文](#繁體中文) | [English](#english)

---

## 繁體中文

### 專案簡介
**TravelTrip** 是一個基於 **.NET 10** 開發的高性能旅遊行程搜尋與推荐系統。本專案的核心競爭力在於採用 **原生 RAG (Retrieval-Augmented Generation)** 技術，透過 **pgvector** 與 **地端 Embedding 模型** 實作深度語義搜尋，達成資料隱私與零成本 AI 整合。

### 技術亮點
- **.NET 10 原生 AI 整合**: 運用 `Microsoft.Extensions.AI` 抽象介面，實現與 AI 模型供應商的解耦，並使用本地模型生成向量。
- **混合搜尋演算法 (Hybrid Search)**: 自定義 `TripRagService` 結合向量相似度與關鍵字權重，顯著提升旅遊景點的匹配精準度。
- **分散式微服務架構**: 
  - **Api 服務**: 負責即時請求與 RAG 檢索。
  - **Worker 服務**: 背景非同步向量化，確保系統高可用。
- **高性能並行優化**: 使用 `Parallel.ForEachAsync` 充分發揮多核 CPU 效能，加速大規模資料初始化。
- **現代化設計模式**: 嚴格遵循 **SOLID 原則**、**Repository Pattern** 與 **Clean Architecture** 思維。

### 系統架構
為了發揮 GitHub 的渲染能力，本專案直接在 README 中嵌入 **Mermaid 動態架構圖**。您可以直接查閱下圖，或開啟 [Draw.io 原始繪圖檔](./TravelTrip_Architecture_Detailed.drawio)。

```mermaid
graph TD
    subgraph Client_Layer [用戶端 - React / Vite]
        UI["UI Components<br/>(Trips, Search, Cart)"]
        State["State Management<br/>(Storage / Context)"]
        Inceptors["HTTP Interceptors<br/>(JWT Injection)"]
        UI --> State
        State --> Inceptors
    end

    subgraph API_Processing [API 中介軟體與流程]
        CORS["CORS Policy"]
        Auth["JWT Authentication<br/>(AuthorizeRole)"]
        ExHandler["Global Exception Handler<br/>(Middleware)"]
        Inceptors -- "HTTP Request" --> CORS
        CORS --> Auth
        Auth --> ExHandler
    end

    subgraph Application_Services [整合服務層]
        TRAG["TripRagService<br/>(Search & Init)"]
        CBOT["ChatbotService<br/>(AI Chat)"]
        BOOK["BookingService<br/>(Order Flow)"]
        CART["CartService<br/>(Redis/DB)"]
        AUTH_S["AuthService<br/>(Identity)"]
        AUDIT["AuditService<br/>(Admin Logs)"]
        EMAIL["EmailService<br/>(SMTP)"]

        ExHandler --> TRAG
        TRAG --> CBOT
        ExHandler --> BOOK
        ExHandler --> AUTH_S
        BOOK -.-> AUDIT
    end

    subgraph AI_Infrastructure [地端 AI 核心]
        Embedding["Local Text Embedder<br/>(SmartComponents)"]
        TRAG -- "Generate Query Vector" --> Embedding
        Worker["TravelTrip.Worker"] -- "Batch Vectorization" --> Embedding
    end

    subgraph Persistence_Layer [資料持久化]
        Repo["ITripRepository / TripRepo"]
        Ctx["TravelDbContext<br/>(EF Core 10)"]
        TRAG --> Repo
        BOOK --> Ctx
        AUTH_S --> Ctx
        AUDIT --> Ctx
        Repo --> Ctx
        Ctx -- "Vector Search (+/-)" --> DB[("PostgreSQL<br/>+ pgvector")]
    end
```

---

## English

### Introduction
**TravelTrip** is a high-performance travel search and recommendation engine built on **.NET 10**. It features a native **RAG (Retrieval-Augmented Generation)** implementation using **pgvector** and **local embedding models**, providing deep semantic search without external API dependencies.

### Key Technical Features
- **Native .NET 10 AI Abstractions**: Uses `Microsoft.Extensions.AI` to ensure vendor neutrality and modular AI integration.
- **Hybrid Search Algorithm**: Combines semantic vector search with keyword-based scoring for superior destination discovery.
- **Distributed Microservices Architecture**: Decouples real-time API requests from heavy background vectorization tasks via a standalone Worker service.
- **Concurrency & Scaling**: Leverages `Parallel.ForEachAsync` for high-throughput data processing.
- **Enterprise Design Patterns**: Implements **Repository Pattern**, **Unit of Work** (via EF Core), and **Clean Architecture** principles.

### Architecture
The project leverages GitHub's native Mermaid support for dynamic visualization. You can also view the [Source Draw.io File](./TravelTrip_Architecture_Detailed.drawio).

```mermaid
graph TD
    API["TravelTrip API"] --> Core["TravelTrip.Core"]
    Worker["TravelTrip.Worker"] --> Core
    Core --> DB[("PostgreSQL + pgvector")]
    API -- "RAG Search" --> DB
    Worker -- "Background Vectorization" --> DB
```

---

### Tech Stack
- **Framework**: .NET 10 (ASP.NET Core / Background Worker)
- **Database**: PostgreSQL + pgvector
- **AI**: Microsoft SmartComponents (Local Embedder)
- **Frontend**: React (Vite)
- **Security**: JWT, PBKDF2 Password Hashing
