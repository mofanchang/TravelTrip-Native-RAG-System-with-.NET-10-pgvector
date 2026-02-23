using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TravelTrip.Core.Data;
using TravelTrip.Core.Models;
using TravelTrip.Core.DTOs;

namespace TravelTripApi.Services;

/// <summary>
/// 認證服務
/// 處理使用者註冊、登入、JWT Token 生成等
/// </summary>
public class AuthService
{
    private readonly TravelDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        TravelDbContext context,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// 註冊新使用者
    /// </summary>
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // 檢查 Email 是否已存在
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "此 Email 已被註冊"
                };
            }

            // 建立新使用者
            var user = new UserEntity
            {
                Email = request.Email,
                Username = request.Username,
                FirstName = request.FirstName ?? string.Empty,
                LastName = request.LastName ?? string.Empty,
                Password = HashPassword(request.Password),
                IsActive = true,
                IsStaff = false,
                IsSuperuser = false,
                DateJoined = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"新使用者註冊成功: {user.Email}");

            // 生成 JWT Token
            var token = GenerateJwtToken(user);

            return new AuthResponse
            {
                Success = true,
                Message = "註冊成功",
                Token = token,
                User = MapToUserDto(user)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "註冊失敗");
            return new AuthResponse
            {
                Success = false,
                Message = "註冊失敗，請稍後再試"
            };
        }
    }

    /// <summary>
    /// 使用者登入
    /// </summary>
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            // 查詢使用者
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Email 或密碼錯誤"
                };
            }

            // 驗證密碼
            if (!VerifyPassword(request.Password, user.Password))
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Email 或密碼錯誤"
                };
            }

            // 檢查帳號是否啟用
            if (!user.IsActive)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "此帳號已被停用"
                };
            }

            // 更新最後登入時間
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"使用者登入成功: {user.Email}");

            // 生成 JWT Token
            var token = GenerateJwtToken(user);

            return new AuthResponse
            {
                Success = true,
                Message = "登入成功",
                Token = token,
                User = MapToUserDto(user)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登入失敗");
            return new AuthResponse
            {
                Success = false,
                Message = "登入失敗，請稍後再試"
            };
        }
    }

    /// <summary>
    /// 取得使用者資料
    /// </summary>
    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user != null ? MapToUserDto(user) : null;
    }

    /// <summary>
    /// 生成 JWT Token
    /// </summary>
    public string GenerateJwtToken(UserEntity user)
    {
        var jwtSecret = _configuration["Jwt:Secret"] ?? "your-secret-key-min-32-characters-long-for-security";
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "TravelTripApi";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "TravelTripClient";
        var jwtExpireMinutes = int.Parse(_configuration["Jwt:ExpireMinutes"] ?? "1440"); // 預設 24 小時

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username ?? user.Email),
            new Claim("is_staff", user.IsStaff.ToString()),
            new Claim("is_superuser", user.IsSuperuser.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwtExpireMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// 雜湊密碼（使用 PBKDF2）
    /// 雜湊密碼（使用 PBKDF2）
    /// 使用標準 pbkdf2_sha256 格式
    /// </summary>
    private string HashPassword(string password)
    {
        const int iterations = 600000; // Django 預設迭代次數
        const int saltSize = 16;
        const int hashSize = 32;

        // 生成隨機 Salt
        var salt = new byte[saltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // 使用 PBKDF2 雜湊
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, hashSize);

        // 組合成標準格式: pbkdf2_sha256$iterations$salt$hash
        var saltBase64 = Convert.ToBase64String(salt);
        var hashBase64 = Convert.ToBase64String(hash);

        return $"pbkdf2_sha256${iterations}${saltBase64}${hashBase64}";
    }

    /// <summary>
    /// 驗證密碼
    /// 支援 pbkdf2_sha256 格式
    /// </summary>
    private bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            if (!hashedPassword.StartsWith("pbkdf2_sha256$"))
            {
                // 支援 BCrypt (用於測試或舊資料)
                try {
                    if (BCrypt.Net.BCrypt.Verify(password, hashedPassword)) return true;
                } catch { }
                return false;
            }
            
            var parts = hashedPassword.Split('$');
            if (parts.Length != 4) return false;

            var iterations = int.Parse(parts[1]);
            var salt = Convert.FromBase64String(parts[2]);
            var storedHash = Convert.FromBase64String(parts[3]);

            // 使用相同參數雜湊輸入的密碼
            var computedHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, storedHash.Length);

            // 比較雜湊值
            return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 將 UserEntity 轉換為 UserDto
    /// </summary>
    private UserDto MapToUserDto(UserEntity user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DateJoined = user.DateJoined,
            IsActive = user.IsActive
        };
    }
}
