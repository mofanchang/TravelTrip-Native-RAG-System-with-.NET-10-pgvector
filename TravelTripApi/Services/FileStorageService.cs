namespace TravelTripApi.Services;

/// <summary>
/// 檔案儲存服務介面
/// 可切換本地儲存或雲端儲存
/// </summary>
public interface IFileStorageService
{
    Task<string> UploadFileAsync(IFormFile file, string folder);
    Task<bool> DeleteFileAsync(string fileUrl);
    string GetFileUrl(string fileName, string folder);
}

/// <summary>
/// 本地檔案儲存服務（開發環境）
/// 對應 Django 的 MEDIA_ROOT
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<LocalFileStorageService> _logger;
    private const string UPLOADS_FOLDER = "uploads";

    public LocalFileStorageService(
        IWebHostEnvironment environment,
        ILogger<LocalFileStorageService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// 上傳檔案
    /// </summary>
    public async Task<string> UploadFileAsync(IFormFile file, string folder)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("檔案為空");
            }

            // 驗證檔案類型
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException($"不支援的檔案格式: {extension}");
            }

            // 驗證檔案大小（最大 5MB）
            if (file.Length > 5 * 1024 * 1024)
            {
                throw new ArgumentException("檔案大小不能超過 5MB");
            }

            // 生成唯一檔名（避免重複）
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            
            // 建立完整路徑
            var uploadPath = Path.Combine(
                _environment.WebRootPath, 
                UPLOADS_FOLDER, 
                folder
            );

            // 確保目錄存在
            Directory.CreateDirectory(uploadPath);

            var filePath = Path.Combine(uploadPath, uniqueFileName);

            // 儲存檔案
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation($"檔案上傳成功: {uniqueFileName}");

            // 回傳相對路徑（用於資料庫儲存）
            return $"/{UPLOADS_FOLDER}/{folder}/{uniqueFileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "檔案上傳失敗");
            throw;
        }
    }

    /// <summary>
    /// 刪除檔案
    /// </summary>
    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(fileUrl))
            {
                return false;
            }

            // 移除開頭的 /
            var relativePath = fileUrl.TrimStart('/');
            var filePath = Path.Combine(_environment.WebRootPath, relativePath);

            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath));
                _logger.LogInformation($"檔案刪除成功: {fileUrl}");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"檔案刪除失敗: {fileUrl}");
            return false;
        }
    }

    /// <summary>
    /// 取得檔案 URL
    /// </summary>
    public string GetFileUrl(string fileName, string folder)
    {
        return $"/{UPLOADS_FOLDER}/{folder}/{fileName}";
    }
}

/// <summary>
/// Azure Blob 儲存服務（生產環境）
/// 類似 Django 的 django-storages + Azure
/// </summary>
public class AzureBlobStorageService : IFileStorageService
{
    private readonly ILogger<AzureBlobStorageService> _logger;
    private readonly string _connectionString;
    private readonly string _containerName;

    public AzureBlobStorageService(
        IConfiguration configuration,
        ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;
        _connectionString = configuration["AzureStorage:ConnectionString"] ?? "";
        _containerName = configuration["AzureStorage:ContainerName"] ?? "uploads";
    }

    public async Task<string> UploadFileAsync(IFormFile file, string folder)
    {
        try
        {
            // TODO: 實作 Azure Blob Storage 上傳
            // 需要安裝 Azure.Storage.Blobs NuGet 套件
            
            throw new NotImplementedException("Azure Blob Storage 尚未實作");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure Blob 上傳失敗");
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        throw new NotImplementedException("Azure Blob Storage 尚未實作");
    }

    public string GetFileUrl(string fileName, string folder)
    {
        // Azure Blob URL 格式
        return $"https://{_containerName}.blob.core.windows.net/{folder}/{fileName}";
    }
}
