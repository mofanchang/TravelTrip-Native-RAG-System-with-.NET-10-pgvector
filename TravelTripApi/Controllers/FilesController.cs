using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelTripApi.Services;

namespace TravelTripApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiController]
[Route("api/[controller]")]
public class FilesController : BaseController
{
    private readonly FileUploadService _fileUploadService;
    private readonly ILogger<FilesController> _logger;

    public FilesController(
        FileUploadService fileUploadService,
        ILogger<FilesController> logger)
    {
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    /// <summary>
    /// 上傳行程圖片 (需要 Admin)
    /// POST /api/files/upload/trip
    /// </summary>
    [Authorize]
    [AuthorizeRole(AdminRole.Admin)]
    [HttpPost("upload/trip")]
    public async Task<IActionResult> UploadTripImage([FromForm] IFormFile file)
    {
        if (file == null)
            return BadRequest(ApiResponse<object>.FailureResult("請選擇檔案"));

        var (success, message, filePath) = await _fileUploadService.UploadFileAsync(file, "trips");

        if (success)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var fullUrl = _fileUploadService.GetFileUrl(filePath!, baseUrl);

            return Ok(ApiResponse<object>.SuccessResult(new
            {
                relativePath = filePath,
                fullUrl,
                fileName = Path.GetFileName(filePath)
            }, message));
        }
        
        return BadRequest(ApiResponse<object>.FailureResult(message));
    }

    /// <summary>
    /// 上傳使用者頭像
    /// POST /api/files/upload/avatar
    /// </summary>
    [Authorize]
    [HttpPost("upload/avatar")]
    public async Task<IActionResult> UploadAvatar([FromForm] IFormFile file)
    {
        if (file == null)
            return BadRequest(ApiResponse<object>.FailureResult("請選擇檔案"));

        var (success, message, filePath) = await _fileUploadService.UploadFileAsync(file, "avatars");

        if (success)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var fullUrl = _fileUploadService.GetFileUrl(filePath!, baseUrl);

            return Ok(ApiResponse<object>.SuccessResult(new
            {
                relativePath = filePath,
                fullUrl
            }, message));
        }
        
        return BadRequest(ApiResponse<object>.FailureResult(message));
    }

    /// <summary>
    /// 刪除檔案 (需要 Admin)
    /// DELETE /api/files?path=/uploads/trips/xxx.jpg
    /// </summary>
    [Authorize]
    [AuthorizeRole(AdminRole.Admin)]
    [HttpDelete]
    public async Task<IActionResult> DeleteFile([FromQuery] string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return BadRequest(ApiResponse<object>.FailureResult("請提供檔案路徑"));

        var success = await _fileUploadService.DeleteFileAsync(path);

        if (success)
            return Ok(ApiResponse<object>.SuccessResult(null, "檔案刪除成功"));
        
        return NotFound(ApiResponse<object>.FailureResult("檔案不存在"));
    }
}
