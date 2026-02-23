# 🔧 Moq 擴展方法問題修正

## 問題
```
System.NotSupportedException: Unsupported expression: c => c.GetValue<string>("RagServer:BaseUrl", It.IsAny<string>())
Extension methods (here: ConfigurationBinder.GetValue) may not be used in setup / verification expressions.
```

## 原因
- `GetValue<T>()` 是 IConfiguration 的**擴展方法**
- Moq **無法 Mock 擴展方法**
- 只能 Mock 介面的實際方法

## 解決方案
改用**真實的 Configuration** 實例（使用 In-Memory Collection）：

### ❌ 錯誤做法（Mock 擴展方法）
```csharp
var mockConfig = new Mock<IConfiguration>();
mockConfig.Setup(c => c.GetValue<string>("RagServer:BaseUrl", It.IsAny<string>()))
    .Returns("http://localhost:8000");  // 會失敗！
```

### ✅ 正確做法（使用真實的 Configuration）
```csharp
var configBuilder = new ConfigurationBuilder();
configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
{
    { "RagServer:BaseUrl", "http://localhost:8000" }
});
var configuration = configBuilder.Build();

// 使用真實的 configuration
_ragClient = new RagEmbeddingClient(
    mockHttpClientFactory.Object,
    mockRagClientLogger.Object,
    configuration);  // 真實實例，不是 Mock
```

## 修正的文件
- ✅ `TripsControllerTests.cs`
- ✅ `ChatbotServiceTests.cs`

## 現在執行測試
```bash
cd /Users/Flora/Documents/TRAVELTRIP_NET_VERSION/TravelTripApi.Tests
dotnet test
```

應該通過 86+ 個測試！🎉
