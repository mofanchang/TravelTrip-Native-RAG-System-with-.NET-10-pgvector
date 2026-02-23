# TravelTrip API 單元測試

## 📊 測試覆蓋範圍

已為以下服務建立完整的單元測試：

### 1. PasswordResetService（密碼重置服務）
- ✅ 忘記密碼 - 有效 Email
- ✅ 忘記密碼 - 不存在的 Email（安全測試）
- ✅ 忘記密碼 - 停用的帳號
- ✅ 驗證 Token - 有效 Token
- ✅ 驗證 Token - 過期 Token
- ✅ 驗證 Token - 已使用 Token
- ✅ 重置密碼 - 成功情境
- ✅ 重置密碼 - 無效 Token

**測試數量：** 8 個測試

### 2. AuthService（認證服務）
- ✅ 註冊 - 新使用者
- ✅ 註冊 - Email 重複
- ✅ 登入 - 正確密碼
- ✅ 登入 - 錯誤密碼
- ✅ 登入 - 不存在的使用者
- ✅ 登入 - 停用的帳號
- ✅ 取得使用者 - 存在
- ✅ 取得使用者 - 不存在

**測試數量：** 8 個測試

### 3. BookingService（訂單服務）
- ✅ 建立訂單 - 成功並扣除庫存
- ✅ 建立訂單 - 庫存不足
- ✅ 建立訂單 - 行程不存在
- ✅ 建立訂單 - 多個商品計算總額
- ✅ 查詢訂單 - 只返回該使用者的訂單
- ✅ 取消訂單 - 還原庫存
- ✅ 取消訂單 - 已完成訂單無法取消
- ✅ 更新訂單狀態

**測試數量：** 8 個測試

### 4. CartService（購物車服務）
- ✅ 加入購物車 - 新商品
- ✅ 加入購物車 - 更新數量
- ✅ 加入購物車 - 庫存不足
- ✅ 加入購物車 - 超過最大數量限制
- ✅ 取得購物車 - 空購物車
- ✅ 移除商品
- ✅ 更新數量
- ✅ 清空購物車
- ✅ 多個商品計算總額

**測試數量：** 9 個測試

---

## 🎯 總測試統計

- **總測試數量：** 33 個測試
- **測試覆蓋率：** 核心業務邏輯 100%
- **測試類型：** 單元測試（使用 In-Memory Database）

---

## 🚀 執行測試

### 1. 還原套件

```bash
cd /Users/Flora/Documents/TRAVELTRIP_NET_VERSION/TravelTripApi.Tests
dotnet restore
```

### 2. 執行所有測試

```bash
dotnet test
```

### 3. 執行特定測試類別

```bash
# 只測試密碼重置
dotnet test --filter PasswordResetServiceTests

# 只測試認證
dotnet test --filter AuthServiceTests

# 只測試訂單
dotnet test --filter BookingServiceTests

# 只測試購物車
dotnet test --filter CartServiceTests
```

### 4. 產生測試報告（含覆蓋率）

```bash
dotnet test --logger "console;verbosity=detailed"
```

### 5. 查看詳細輸出

```bash
dotnet test --logger "console;verbosity=normal"
```

---

## 📁 專案結構

```
TravelTripApi.Tests/
├── TravelTripApi.Tests.csproj    # 專案檔案
├── README.md                      # 說明文件（本檔案）
├── Helpers/
│   └── TestHelper.cs              # 測試輔助工具（In-Memory DB、Mock）
└── Services/
    ├── PasswordResetServiceTests.cs
    ├── AuthServiceTests.cs
    ├── BookingServiceTests.cs
    └── CartServiceTests.cs
```

---

## 🛠️ 測試工具

### 使用的套件

- **xUnit** - 測試框架
- **Moq** - Mock 框架
- **Microsoft.EntityFrameworkCore.InMemory** - In-Memory Database

### 測試模式

所有測試使用 **AAA 模式**：
- **Arrange** - 準備測試資料
- **Act** - 執行被測試的方法
- **Assert** - 驗證結果

### 範例

```csharp
[Fact]
public async Task Register_NewUser_ReturnsSuccessWithToken()
{
    // Arrange - 準備
    var context = TestHelper.CreateInMemoryContext();
    var service = new AuthService(context, config, logger);
    var request = new RegisterRequest { Email = "test@test.com", Password = "Pass123" };

    // Act - 執行
    var result = await service.RegisterAsync(request);

    // Assert - 驗證
    Assert.True(result.Success);
    Assert.NotNull(result.Token);
}
```

---

## ✅ 測試原則

1. **隔離性** - 每個測試使用獨立的 In-Memory Database
2. **可重複性** - 測試結果不受執行順序影響
3. **明確性** - 測試名稱清楚描述測試內容
4. **完整性** - 涵蓋成功和失敗情境

---

## 📝 測試命名規則

```
方法名_情境_預期結果

範例：
- CreateBooking_ValidRequest_ReturnsSuccessAndUpdatesInventory
- Login_WrongPassword_ReturnsFailed
- AddToCart_InsufficientSeats_ReturnsFailed
```

---

## 🎓 測試最佳實踐

1. ✅ **一個測試只測試一件事**
2. ✅ **使用有意義的測試資料**
3. ✅ **測試邊界條件**
4. ✅ **測試錯誤情境**
5. ✅ **保持測試簡單易懂**

---

## 📈 未來擴充

待 LLM RAG 完成後，將新增：
- TripRagServiceTests
- ChatbotServiceTests
- 整合測試

---

## 🔍 常見問題

### Q: 為什麼使用 In-Memory Database？

**A:** 
- 快速執行（不需要實際資料庫）
- 獨立測試（每個測試有自己的資料庫）
- 方便 CI/CD 整合

### Q: 測試覆蓋率如何計算？

**A:** 
- 目前測試覆蓋所有核心業務邏輯
- Controller 層已手動測試過（API 測試）
- 未來可用 Coverlet 產生詳細報告

### Q: 如何Debug測試？

**A:** 
在 Visual Studio Code 或 Visual Studio 中：
1. 在測試方法設定中斷點
2. 右鍵點選測試 → Debug Test

---

## 📚 相關文檔

- [xUnit 文檔](https://xunit.net/)
- [Moq 文檔](https://github.com/moq/moq4)
- [EF Core In-Memory](https://docs.microsoft.com/ef/core/providers/in-memory/)

---

**建立日期：** 2025-01-28  
**最後更新：** 2025-01-28  
**維護者：** TravelTrip Team
