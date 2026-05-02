using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockAlert.Application.Interfaces;

namespace StockAlert.Infrastructure.BackgroundServices;

public class StockSyncWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StockSyncWorker> _logger;

    public StockSyncWorker(IServiceProvider serviceProvider, ILogger<StockSyncWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("StockSyncWorker: Starting sync with SmartTrade at {time}", DateTimeOffset.Now);

            using (var scope = _serviceProvider.CreateScope())
            {
                var externalService = scope.ServiceProvider.GetRequiredService<IExternalStockService>();
                var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                try 
                {
                    // 1. Pull data from SmartTrade
                    var externalProducts = await externalService.SyncFromExternalAsync();

                    // 2. Logic to update your SQL Express local DB
                    // (e.g., Update local StockQuantity if it differs from SmartTrade)
                    
                    _logger.LogInformation("StockSyncWorker: Sync completed successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "StockSyncWorker: Error occurred during sync.");
                }
            }

            // Wait 30 minutes before running again
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }
}