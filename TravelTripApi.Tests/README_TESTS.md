# 🧪 TravelTrip API 測試指南

## 📋 目錄
- [快速開始](#快速開始)
- [測試結構](#測試結構)
- [執行測試](#執行測試)
- [查看覆蓋率](#查看覆蓋率)
- [新增測試](#新增測試)
- [常見問題](#常見問題)

---

## 🚀 快速開始

### 執行所有測試
```bash
cd /Users/Flora/Documents/TRAVELTRIP_NET_VERSION/TravelTripApi.Tests
dotnet test --collect:"XPlat Code Coverage"
```

### 使用測試腳本（推薦）
```bash
chmod +x run_tests.sh
./run_tests.sh
```

---

## 📁 測試結構

```
TravelTripApi.Tests/
├── Controllers/               # Controller 層測試 (新增)
│   ├── AuthControllerTests.cs       # 認證 API 測試
│   ├── TripsControllerTests.cs      # 行程 API 測試
│   ├── BookingsControllerTests.cs   # 訂單 API 測試
│   └── CartControllerTests.cs       # 購物車 API 測試
│
├── Services/                  # Service 層測試
│   ├── AuthServiceTests.cs          # 認證服務測試
│   ├── BookingServiceTests.cs       # 訂單服務測試
│   ├── CartServiceTests.cs          # 購物車服務測試
│   ├── ChatbotServiceTests.cs       # 聊天機器人測試 (新增)
│   ├── PasswordResetServiceTests.cs # 密碼重設測試
│   └── TripRagServiceTests.cs       # RAG 搜尋測試
│
├── Performance/               # 性能測試
│   └── RagPerformanceTests.cs       # RAG 性能測試
│
├── Helpers/                   # 測試輔助工具
│   └── TestHelper.cs                # 共用測試工具
│
├── TestHelpers/               # 測試輔助類別
│   └── AsyncQueryHelpers.cs         # 異步查詢輔助
│
├── run_tests.sh              # 測試執行腳本 (新增)
├── TEST_SUITE_SUMMARY.md     # 測試套件總結 (新增)
└── README_TESTS.md           # 本文件 (新增)
```

---

## 🧪 執行測試

### 1. 執行所有測試
```bash
dotnet test
```

### 2. 執行特定類別的測試

#### 只測試 Controllers
```bash
dotnet test --filter "FullyQualifiedName~Controllers"
```

#### 只測試 Services
```bash
dotnet test --filter "FullyQualifiedName~Services"
```

### 3. 執行單一測試文件

```bash
# AuthController 測試
dotnet test --filter "FullyQualifiedName~AuthControllerTests"

# TripsController 測試
dotnet test --filter "FullyQualifiedName~TripsControllerTests"

# BookingsController 測試
dotnet test --filter "FullyQualifiedName~BookingsControllerTests"

# CartController 測試
dotnet test --filter "FullyQualifiedName~CartControllerTests"

# ChatbotService 測試
dotnet test --filter "FullyQualifiedName~ChatbotServiceTests"
```

### 4. 執行單一測試方法
```bash
dotnet test --filter "FullyQualifiedName~AuthControllerTests.Login_ValidCredentials_ReturnsOkWithToken"
```

### 5. 詳細輸出
```bash
dotnet test --logger "console;verbosity=detailed"
```

---

## 📊 查看覆蓋率

### 步驟 1: 執行測試並收集覆蓋率
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### 步驟 2: 生成 HTML 報告
```bash
~/.dotnet/tools/reportgenerator \
  -reports:"./TestResults/*/coverage.cobertura.xml" \
  -targetdir:"coveragereport" \
  -reporttypes:Html
```

### 步驟 3: 開啟報告
```bash
open coveragereport/index.html
```

### 一鍵完成（使用測試腳本）
```bash
./run_tests.sh
```

---

## ✍️ 新增測試

### 1. 為新的 Controller 創建測試

```csharp
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TravelTripApi.Controllers;
using TravelTripApi.Services;

namespace TravelTripApi.Tests.Controllers;

public class YourControllerTests
{
    private readonly Mock<YourService> _mockService;
    private readonly Mock<ILogger<YourController>> _mockLogger;
    private readonly YourController _controller;

    public YourControllerTests()
    {
        _mockService = new Mock<YourService>();
        _mockLogger = new Mock<ILogger<YourController>>();
        _controller = new YourController(_mockService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task YourMethod_ValidInput_ReturnsOk()
    {
        // Arrange
        _mockService.Setup(s => s.YourMethod())
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.YourMethod();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }
}
```

### 2. 為新的 Service 創建測試

```csharp
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using TravelTripApi.Services;
using TravelTripApi.Data;

namespace TravelTripApi.Tests.Services;

public class YourServiceTests
{
    private readonly TravelDbContext _context;
    private readonly Mock<ILogger<YourService>> _mockLogger;
    private readonly YourService _service;

    public YourServiceTests()
    {
        var options = new DbContextOptionsBuilder<TravelDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new TravelDbContext(options);
        _mockLogger = new Mock<ILogger<YourService>>();
        _service = new YourService(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task YourMethod_ValidInput_ReturnsExpectedResult()
    {
        // Arrange
        // 設置測試數據

        // Act
        var result = await _service.YourMethod();

        // Assert
        Assert.NotNull(result);
        // 更多斷言...
    }
}
```

---

## 🎯 測試最佳實踐

### 1. AAA 模式
```csharp
[Fact]
public async Task Method_Scenario_ExpectedResult()
{
    // Arrange - 準備測試數據和 Mock
    var request = new Request { ... };
    _mockService.Setup(...).ReturnsAsync(...);

    // Act - 執行被測試的方法
    var result = await _controller.Method(request);

    // Assert - 驗證結果
    Assert.IsType<OkObjectResult>(result);
}
```

### 2. 測試命名
```
[MethodName]_[Scenario]_[ExpectedResult]

範例:
- Login_ValidCredentials_ReturnsOkWithToken
- AddToCart_InvalidQuantity_ReturnsBadRequest
- GetBooking_InvalidId_ReturnsNotFound
```

### 3. 測試覆蓋
每個公開方法至少應該有：
- ✅ 成功情境測試 (Happy Path)
- ✅ 失敗情境測試 (Error Cases)
- ✅ 邊界條件測試 (Edge Cases)
- ✅ 異常處理測試 (Exception Handling)

### 4. Theory 測試
使用 Theory 測試多個輸入值：
```csharp
[Theory]
[InlineData(1)]
[InlineData(5)]
[InlineData(10)]
public async Task AddToCart_ValidQuantities_ReturnsOk(int quantity)
{
    // ...
}
```

---

## 🐛 常見問題

### Q: 測試執行很慢
**A:** 使用 `--no-build` 選項跳過編譯：
```bash
dotnet test --no-build
```

### Q: 覆蓋率報告生成失敗
**A:** 確保已安裝 reportgenerator：
```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

### Q: In-Memory Database 測試失敗
**A:** 確保每個測試使用唯一的資料庫名稱：
```csharp
.UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
```

### Q: Mock 設置不生效
**A:** 檢查：
1. Mock 的方法簽名是否完全匹配
2. 是否使用了 `It.IsAny<T>()` 
3. 是否在 Act 之前設置 Mock

### Q: JWT 認證測試失敗
**A:** 確保設置了 User Claims：
```csharp
var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, "1")
};
var identity = new ClaimsIdentity(claims, "TestAuthType");
var claimsPrincipal = new ClaimsPrincipal(identity);

_controller.ControllerContext = new ControllerContext
{
    HttpContext = new DefaultHttpContext { User = claimsPrincipal }
};
```

---

## 📈 測試統計

### 當前測試數量
- **總測試數**: 129 個
- **Controllers 測試**: 71 個
- **Services 測試**: 58 個

### 覆蓋率目標
- **當前**: 18.4% → **目標**: 55-65%
- **Controllers**: 0% → **目標**: 85-95%
- **Services**: 75% → **維持**: 75-90%

---

## 🔗 相關資源

### 官方文檔
- [xUnit 官方文檔](https://xunit.net/)
- [Moq 文檔](https://github.com/moq/moq4/wiki)
- [ASP.NET Core 測試](https://docs.microsoft.com/en-us/aspnet/core/test/)
- [EF Core 測試](https://docs.microsoft.com/en-us/ef/core/testing/)

### 測試工具
- [Coverlet](https://github.com/coverlet-coverage/coverlet) - 覆蓋率收集
- [ReportGenerator](https://github.com/danielpalme/ReportGenerator) - 覆蓋率報告
- [xUnit](https://xunit.net/) - 測試框架
- [Moq](https://github.com/moq/moq4) - Mock 框架

---

## 🎉 總結

這個測試套件提供了：
- ✅ 完整的 Controller 層測試
- ✅ 完整的 Service 層測試  
- ✅ 認證與授權測試
- ✅ 錯誤處理測試
- ✅ 邊界條件測試
- ✅ 異步操作測試

繼續保持良好的測試習慣，確保程式碼品質！💪
