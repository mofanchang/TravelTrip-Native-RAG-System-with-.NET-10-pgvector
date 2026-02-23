# 📁 .NET 檔案上傳與靜態檔案處理指南

## 🎯 從 Django 到 .NET 的檔案處理對照

### Django 的做法
```python
# settings.py
MEDIA_ROOT = os.path.join(BASE_DIR, 'media')
MEDIA_URL = '/media/'

# models.py
class Trip(models.Model):
    image = models.ImageField(upload_to='trips/')
    
# urls.py
from django.conf import settings
from django.conf.urls.static import static

urlpatterns = [
    # ...
] + static(settings.MEDIA_URL, document_root=settings.MEDIA_ROOT)
```

### .NET 的做法

---

## 📂 方案 1：本地檔案系統（開發環境）

### 目錄結構
```
TravelTripApi/
├── wwwroot/                    # 靜態檔案根目錄（對應 Django 的 MEDIA_ROOT）
│   ├── uploads/                # 上傳檔案目錄
│   │   ├── trips/             # 行程圖片
│   │   ├── avatars/           # 使用者頭像
│   │   └── temp/              # 臨時檔案
│   └── static/                # 其他靜態資源（CSS, JS）
└── ...
```

### 在 Program.cs 中配置

```csharp
// 1. 註冊檔案上傳服務
builder.Services.AddScoped<FileUploadService>();

// 2. 啟用靜態檔案服務
var app = builder.Build();

// 重要：必須在 UseRouting 之前
app.UseStaticFiles();  // 啟用 wwwroot 目錄的靜態檔案服務

// 或自訂路徑
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads")),
    RequestPath = "/uploads"
});
```

### 使用方式

#### 上傳檔案
```bash
# 使用 Postman 或 curl 上傳
curl -X POST http://localhost:5000/api/files/upload/trip \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "file=@/path/to/image.jpg"

# 回應
{
  "message": "上傳成功",
  "relativePath": "/uploads/trips/abc123.jpg",
  "fullUrl": "http://localhost:5000/uploads/trips/abc123.jpg",
  "fileName": "abc123.jpg"
}
```

#### 存取檔案
```
http://localhost:5000/uploads/trips/abc123.jpg
```

#### 在 Model 中使用
```csharp
// 建立行程時儲存圖片路徑
var trip = new TripEntity
{
    Name = "京都賞櫻",
    Image = "/uploads/trips/abc123.jpg"  // 儲存相對路徑
};
```

---

## 📂 方案 2：雲端儲存（生產環境推薦）

### 使用 Azure Blob Storage

#### 1. 安裝套件
```bash
dotnet add package Azure.Storage.Blobs
```

#### 2. 建立 Azure Blob Service

```csharp
// Services/AzureBlobService.cs
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

public class AzureBlobService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName = "trip-images";

    public AzureBlobService(IConfiguration configuration)
    {
        var connectionString = configuration["Azure:Storage:ConnectionString"];
        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public async Task<string> UploadAsync(IFormFile file, string folder)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

        var fileName = $"{folder}/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var blobClient = containerClient.GetBlobClient(fileName);

        using (var stream = file.OpenReadStream())
        {
            await blobClient.UploadAsync(stream, true);
        }

        return blobClient.Uri.ToString();
    }

    public async Task<bool> DeleteAsync(string blobUrl)
    {
        var uri = new Uri(blobUrl);
        var blobName = uri.Segments[^1];
        
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        return await blobClient.DeleteIfExistsAsync();
    }
}
```

#### 3. 在 appsettings.json 配置
```json
{
  "Azure": {
    "Storage": {
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=..."
    }
  }
}
```

---

## 📂 方案 3：混合方案（推薦）

開發環境用本地檔案，生產環境用雲端儲存。

### 建立抽象介面

```csharp
// Interfaces/IFileStorageService.cs
public interface IFileStorageService
{
    Task<(bool Success, string Message, string? Url)> UploadAsync(
        IFormFile file, string folder);
    Task<bool> DeleteAsync(string fileUrl);
}

// Services/LocalFileStorageService.cs
public class LocalFileStorageService : IFileStorageService
{
    // 實作本地檔案儲存
}

// Services/AzureFileStorageService.cs
public class AzureFileStorageService : IFileStorageService
{
    // 實作 Azure Blob 儲存
}

// Program.cs 中根據環境註冊
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
}
else
{
    builder.Services.AddScoped<IFileStorageService, AzureFileStorageService>();
}
```

---

## 🔄 從 Django 遷移 media 檔案

### 步驟 1：複製現有檔案

```bash
# 假設 Django 的 media 目錄在
/Users/Flora/Documents/TRAVELTRIP_PROJECT/media/

# 複製到 .NET 專案
cp -r /Users/Flora/Documents/TRAVELTRIP_PROJECT/media/trips/ \
     /Users/Flora/Documents/TRAVELTRIP_NET_VERSION/TravelTripApi/wwwroot/uploads/trips/
```

### 步驟 2：更新資料庫中的路徑

Django 儲存的路徑格式：
```
trips/image_2024.jpg
```

.NET 需要的路徑格式：
```
/uploads/trips/image_2024.jpg
```

**SQL 更新腳本：**
```sql
-- PostgreSQL
UPDATE trips_trip 
SET image = '/uploads/' || image 
WHERE image NOT LIKE '/uploads/%';
```

或在程式中處理：
```csharp
// Migration 或啟動時執行
var trips = await _context.Trips.Where(t => !t.Image.StartsWith("/uploads/")).ToListAsync();
foreach (var trip in trips)
{
    trip.Image = $"/uploads/{trip.Image}";
}
await _context.SaveChangesAsync();
```

---

## 🎨 前端使用範例

### HTML
```html
<img src="http://localhost:5000/uploads/trips/abc123.jpg" alt="行程圖片">
```

### React
```jsx
const TripCard = ({ trip }) => {
  const imageUrl = `${process.env.REACT_APP_API_URL}${trip.image}`;
  
  return (
    <img src={imageUrl} alt={trip.name} />
  );
};
```

### Vue
```vue
<template>
  <img :src="getImageUrl(trip.image)" :alt="trip.name">
</template>

<script>
export default {
  methods: {
    getImageUrl(relativePath) {
      return `${process.env.VUE_APP_API_URL}${relativePath}`;
    }
  }
}
</script>
```

---

## 🔒 安全性考量

### 1. 檔案大小限制
```csharp
// Program.cs
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB
});
```

### 2. 檔案類型驗證
```csharp
private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
private readonly string[] _allowedMimeTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };

// 驗證副檔名
var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
if (!_allowedExtensions.Contains(extension))
{
    return BadRequest("不支援的檔案格式");
}

// 驗證 MIME Type
if (!_allowedMimeTypes.Contains(file.ContentType))
{
    return BadRequest("不支援的檔案類型");
}
```

### 3. 檔名處理（防止路徑穿越攻擊）
```csharp
// ✅ 使用 GUID 作為檔名
var fileName = $"{Guid.NewGuid()}{extension}";

// ❌ 不要直接使用使用者上傳的檔名
var fileName = file.FileName; // 危險！
```

### 4. 病毒掃描（生產環境）
```csharp
// 可整合 ClamAV 或其他防毒軟體
public async Task<bool> ScanFileAsync(Stream fileStream)
{
    // 實作病毒掃描邏輯
}
```

---

## 📊 效能優化

### 1. 圖片壓縮
```bash
dotnet add package SixLabors.ImageSharp
```

```csharp
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

public async Task<string> CompressAndSaveAsync(IFormFile file)
{
    using var image = await Image.LoadAsync(file.OpenReadStream());
    
    // 調整大小
    image.Mutate(x => x.Resize(new ResizeOptions
    {
        Size = new Size(1200, 800),
        Mode = ResizeMode.Max
    }));
    
    // 儲存為 WebP（更小的檔案大小）
    var fileName = $"{Guid.NewGuid()}.webp";
    var filePath = Path.Combine(_uploadsPath, fileName);
    
    await image.SaveAsWebpAsync(filePath);
    
    return fileName;
}
```

### 2. CDN 整合
```csharp
// 使用 Azure CDN 或 Cloudflare
public string GetCdnUrl(string relativePath)
{
    var cdnBaseUrl = _configuration["CDN:BaseUrl"];
    return $"{cdnBaseUrl}{relativePath}";
}
```

---

## 🧪 測試檔案上傳

### 使用 Postman
```
POST http://localhost:5000/api/files/upload/trip
Headers:
  Authorization: Bearer YOUR_JWT_TOKEN
Body:
  form-data
  Key: file (type: File)
  Value: [選擇檔案]
```

### 使用 curl
```bash
curl -X POST http://localhost:5000/api/files/upload/trip \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "file=@/path/to/image.jpg"
```

### 使用 C# HttpClient
```csharp
using var client = new HttpClient();
client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", token);

using var content = new MultipartFormDataContent();
using var fileStream = File.OpenRead("image.jpg");
content.Add(new StreamContent(fileStream), "file", "image.jpg");

var response = await client.PostAsync(
    "http://localhost:5000/api/files/upload/trip", 
    content);
```

---

## 📝 總結對照表

| 功能 | Django | .NET |
|------|--------|------|
| **靜態檔案目錄** | `MEDIA_ROOT` | `wwwroot/uploads` |
| **URL 前綴** | `MEDIA_URL = '/media/'` | `/uploads/` |
| **上傳處理** | `ImageField` 自動處理 | `FileUploadService` |
| **檔案存取** | `static()` helper | `UseStaticFiles()` |
| **雲端儲存** | django-storages | Azure.Storage.Blobs |

---

## 🎯 建議方案

**面試展示用（現在）：**
- ✅ 使用方案 1（本地檔案系統）
- ✅ 已建立 FileUploadService 和 FilesController
- ✅ 配置 wwwroot/uploads 目錄

**實際專案部署：**
- 🚀 使用方案 3（混合方案）
- 開發：本地檔案
- 生產：Azure Blob Storage + CDN

---

需要我幫你：
1. 更新 Program.cs 註冊 FileUploadService？
2. 建立檔案遷移腳本？
3. 加入圖片壓縮功能？

選一個，我馬上幫你完成！
