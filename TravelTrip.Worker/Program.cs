using TravelTrip.Worker;
using TravelTrip.Core.Data;
using TravelTrip.Core.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

var builder = Host.CreateApplicationBuilder(args);

// 1. 資料庫配置
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? 
                      throw new InvalidOperationException("Connection string 'DefaultConnection' not found."); 

builder.Services.AddDbContext<TravelDbContext>(options =>
    options.UseNpgsql(connectionString, o => o.UseVector())
);

// 2. AI & Repository 註冊
builder.Services.AddLocalTextEmbedder();

builder.Services.AddScoped<ITripRepository, TripRepository>();

// 3. 註冊 Worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
