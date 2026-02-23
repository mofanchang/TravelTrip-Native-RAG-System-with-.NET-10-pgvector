using TravelTrip.Core.Data.Repositories;
using Microsoft.Extensions.AI;

namespace TravelTrip.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

    public Worker(
        ILogger<Worker> logger, 
        IServiceProvider serviceProvider,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _embeddingGenerator = embeddingGenerator;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TravelTrip RAG 處理背景服務已啟動");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var repository = scope.ServiceProvider.GetRequiredService<ITripRepository>();
                    
                    // 1. 抓取尚未向量化的行程
                    var trips = await repository.GetTripsToVectorizeAsync(10);

                    if (trips.Any())
                    {
                        _logger.LogInformation("發現 {Count} 個新行程，正在進行向量化...", trips.Count);

                        // 2. 並行生成向量
                        await Parallel.ForEachAsync(trips, stoppingToken, async (trip, ct) =>
                        {
                            var text = $"{trip.Name} {trip.Description} {trip.Country} {trip.City} {trip.Keywords}";
                            var result = await _embeddingGenerator.GenerateAsync(new[] { text }, cancellationToken: ct);
                            trip.Embedding = new Pgvector.Vector(result[0].Vector.ToArray());
                            await repository.UpdateTripAsync(trip);
                        });

                        // 3. 儲存更動
                        await repository.SaveChangesAsync();
                        _logger.LogInformation("向量化處理完成");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "背景處理發生錯誤");
            }

            // 4. 每 30 秒巡檢一次 (面試時可以說這可以依據 Queue 訊號動態觸發)
            await Task.Delay(30000, stoppingToken);
        }
    }
}
