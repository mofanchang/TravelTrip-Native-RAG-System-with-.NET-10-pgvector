namespace TravelTripApi.Services;

/// <summary>
/// 檔案上傳服務
/// 處理圖片和其他檔案的上傳、儲存、刪除
/// </summary>
public class FileUploadService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileUploadService> _logger;
    private readonly long _maxFileSize = 10 * 1024 * 1024; // 10MB
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    public FileUploadService(
        IWebHostEnvironment environment,
        ILogger<FileUploadService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// 上傳檔案
    /// </summary>
    /// <param name="file">上傳的檔案</param>
    /// <param name="folder">儲存的子資料夾（如 "trips"）</param>
    /// <returns>檔案的相對路徑（URL）</returns>
    public async Task<(bool Success, string Message, string? FilePath)> UploadFileAsync(
        IFormFile file, 
        string folder = "trips")
    {
        try
        {
            // 1. 驗證檔案
            if (file == null || file.Length == 0)
            {
                return (false, "沒有選擇檔案", null);
            }

            // 2. 檢查檔案大小
            if (file.Length > _maxFileSize)
            {
                return (false, $"檔案大小不能超過 {_maxFileSize / 1024 / 1024}MB", null);
            }

            // 3. 檢查副檔名
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                return (false, $"不支援的檔案格式。允許的格式：{string.Join(", ", _allowedExtensions)}", null);
            }

            // 4. 生成唯一檔名（避免覆蓋）
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";

            // 5. 建立儲存路徑
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(uploadsFolder); // 確保目錄存在

            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // 6. 儲存檔案
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 7. 回傳相對路徑（URL 格式）
            var relativePath = $"/uploads/{folder}/{uniqueFileName}";

            _logger.LogInformation($"檔案上傳成功: {relativePath}");

            return (true, "上傳成功", relativePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "檔案上傳失敗");
            return (false, "檔案上傳失敗", null);
        }
    }

    /// <summary>
    /// 刪除檔案
    /// </summary>
    public async Task<bool> DeleteFileAsync(string relativePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return false;

            // 移除開頭的 /
            relativePath = relativePath.TrimStart('/');

            var filePath = Path.Combine(_environment.WebRootPath, relativePath);

            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath));
                _logger.LogInformation($"檔案刪除成功: {relativePath}");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"檔案刪除失敗: {relativePath}");
            return false;
        }
    }

    /// <summary>
    /// 取得檔案的完整 URL
    /// </summary>
    public string GetFileUrl(string relativePath, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return string.Empty;

        return $"{baseUrl.TrimEnd('/')}{relativePath}";
    }

    /// <summary>
    /// 驗證是否為圖片檔案
    /// </summary>
    public bool IsImageFile(IFormFile file)
    {
        if (file == null)
            return false;

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return _allowedExtensions.Contains(extension);
    }
}
