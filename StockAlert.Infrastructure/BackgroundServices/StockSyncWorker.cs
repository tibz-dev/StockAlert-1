using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockAlert.Application.Interfaces;
using StockAlert.Domain.Entities;

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
                    int discrepancyCount = 0;

                    // 2. Loop through each external product
                    foreach (var extProduct in externalProducts)
                    {
                        // Find matching local product (Assuming Name or ExternalId is the unique link)
                        var localProduct = await dbContext.Products
                            .FirstOrDefaultAsync(p => p.Name == extProduct.Name, stoppingToken);

                        if (localProduct != null)
                        {
                            if (localProduct.StockQuantity != extProduct.StockQuantity)
                            {
                                int difference = extProduct.StockQuantity - localProduct.StockQuantity;

                                // Create the Audit entry for the reconciliation
                                var syncLog = new AuditLog
                                {
                                    EntityName = "Product",
                                    Action = "Reconciliation",
                                    UserId = "SYSTEM_SYNC",
                                    Changes = $"{localProduct.Name}: Adjusted by {difference} units to match SmartTrade.",
                                    Timestamp = DateTime.UtcNow
                                };

                                localProduct.StockQuantity = extProduct.StockQuantity;
                                dbContext.AuditLogs.Add(syncLog);
                                discrepancyCount++;
                            }
                        }
                    }

                    if (discrepancyCount > 0)
                    {
                        await dbContext.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("StockSyncWorker: Sync completed. Fixed {count} discrepancies.", discrepancyCount);
                    }
                    else
                    {
                        _logger.LogInformation("StockSyncWorker: Sync completed. No discrepancies found.");
                    }
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