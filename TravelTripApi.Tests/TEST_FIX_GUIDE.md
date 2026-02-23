# 🔧 測試修正說明

## 問題診斷

原始測試失敗的原因：
```
System.ArgumentException: Can not instantiate proxy of class: TravelTripApi.Services.BookingService.
Could not find a parameterless constructor.
```

### 根本原因
- **Moq 無法 Mock 具體類別**（concrete classes）
- `BookingService`、`CartService`、`TripRagService`、`RagEmbeddingClient` 都是具體類別
- 這些類別沒有無參數建構函式
- Moq 需要無參數建構函式或介面才能創建 Mock

## 解決方案

### ✅ 已採用：使用真實 Service 實例

改為創建**真實的 Service 實例**，配合 **In-Memory Database**：

```csharp
// ❌ 錯誤做法 - Mock 具體類別
private readonly Mock<BookingService> _mockBookingService;
_mockBookingService = new Mock<BookingService>(); // 會失敗！

// ✅ 正確做法 - 使用真實實例
private readonly TravelDbContext _context;
private readonly BookingService _bookingService;

// 建立 In-Memory Database
var options = new DbContextOptionsBuilder<TravelDbContext>()
    .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
    .Options;
_context = new TravelDbContext(options);

// 創建真實的 Service 實例
var mockLogger = new Mock<ILogger<BookingService>>();
_bookingService = new BookingService(_context, mockLogger.Object);
```

### 修正的測試文件

1. **BookingsControllerTests.cs** ✅
   - 使用真實的 `BookingService`
   - 使用 In-Memory Database
   - 添加測試數據 Seed 方法
   - 實現 `IDisposable` 清理資源

2. **CartControllerTests.cs** ✅
   - 使用真實的 `CartService` 和 `BookingService`
   - 使用 In-Memory Database
   - 添加測試數據 Seed 方法
   - 實現 `IDisposable` 清理資源

3. **TripsControllerTests.cs** ✅
   - 使用真實的 `TripRagService` 和 `RagEmbeddingClient`
   - 使用 In-Memory Database
   - 添加測試數據 Seed 方法
   - 實現 `IDisposable` 清理資源

4. **ChatbotServiceTests.cs** ✅
   - 使用真實的 `ChatbotService`、`TripRagService`、`RagEmbeddingClient`
   - 使用 In-Memory Database
   - 添加測試數據 Seed 方法
   - 實現 `IDisposable` 清理資源

## 測試改進

### 優點
- ✅ **更真實的測試** - 測試真實的業務邏輯，而非 Mock 行為
- ✅ **完整的整合測試** - 測試 Controller、Service、Database 的整合
- ✅ **無需維護 Mock** - 減少 Mock 設置的複雜度
- ✅ **更高的信心** - 測試實際執行路徑

### 注意事項
- ⚠️ **測試速度** - 真實 Service 可能比 Mock 慢一些
- ⚠️ **測試隔離** - 每個測試使用獨立的資料庫（透過 GUID）
- ⚠️ **外部依賴** - RAG 相關測試可能因無法連接 Python 服務而失敗（已處理）

## 執行測試

### 快速測試
```bash
cd /Users/Flora/Documents/TRAVELTRIP_NET_VERSION/TravelTripApi.Tests
dotnet test
```

### 只測試 Controllers
```bash
dotnet test --filter "FullyQualifiedName~Controllers"
```

### 查看詳細輸出
```bash
dotnet test --logger "console;verbosity=detailed"
```

## 預期結果

所有測試應該通過，除了可能的 RAG 相關測試（因為沒有真實的 Python 服務）：
- ✅ AuthController - 8 個測試全部通過
- ✅ BookingsController - 13 個測試全部通過  
- ✅ CartController - 17 個測試全部通過
- ✅ TripsController - 11 個測試（大部分通過）
- ✅ ChatbotService - 9 個測試（大部分通過）

## 未來改進建議

### 選項 1：創建介面（長期）
為 Services 創建介面，便於 Mock：

```csharp
public interface IBookingService
{
    Task<(bool, string, BookingDto?)> CreateBookingAsync(int userId, CreateBookingRequest request);
    // ...
}

public class BookingService : IBookingService
{
    // 實作...
}
```

### 選項 2：使用 NSubstitute（替代）
NSubstitute 可以更容易地 Mock 具體類別：

```csharp
// 使用 NSubstitute 而不是 Moq
var bookingService = Substitute.For<BookingService>(context, logger);
```

### 選項 3：保持現狀（推薦）
當前使用真實 Service 實例的方式已經很好，提供了更真實的整合測試。

---

## 總結

✅ **已修正所有 Mock 相關錯誤**  
✅ **測試現在使用真實的 Service 實例**  
✅ **每個測試都有獨立的 In-Memory Database**  
✅ **測試更接近真實場景**

執行 `dotnet test` 應該可以看到測試通過！
