using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.AI;
using TravelTrip.Core.Data;
using TravelTripApi.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. CORS 配置
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000") 
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 2. Swagger 配置
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "TravelTrip AI API (v10)", 
        Version = "v1",
        Description = "原生 RAG 系統：.NET 10 + EF Core 10 + Microsoft.Extensions.AI"
    });
});

// 3. 資料庫連線
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? 
                      throw new InvalidOperationException("Connection string 'DefaultConnection' not found."); 

builder.Services.AddDbContext<TravelDbContext>(options =>
    options.UseNpgsql(connectionString, o => o.UseVector())
);

// 4. JWT 認證
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret key not found.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"]
        };
    });

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

// 5. .NET 10 原生 AI & RAG 服務註冊
// 使用 Microsoft SmartComponents (完全免費、地端運行、384 維度)
builder.Services.AddLocalTextEmbedder(); // 註冊地端向量引擎

builder.Services.AddScoped<TravelTrip.Core.Data.Repositories.ITripRepository, TravelTrip.Core.Data.Repositories.TripRepository>();
builder.Services.AddScoped<TripRagService>(); // 原生 RAG 核心服務 (主要用於搜尋)

// 註冊其他服務
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<ChatbotService>();
builder.Services.AddScoped<FileUploadService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<PasswordResetService>();

var app = builder.Build();

// 6. 分散式架構：RAG 初始化已移至 TravelTrip.Worker 背景服務
// API 只負責提供搜尋與 CRUD 端點，不處理耗時的向量生成場景

app.UseMiddleware<TravelTripApi.Middleware.GlobalExceptionMiddleware>();
app.UseCors("AllowFrontend");
app.UseStaticFiles(); 
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
