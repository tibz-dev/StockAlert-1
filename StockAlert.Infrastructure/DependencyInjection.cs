using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockAlert.Application.Interfaces;
using StockAlert.Infrastructure.Persistence;
using StockAlert.Infrastructure.Services;

namespace StockAlert.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IProductService, ProductService>();

        
        services.AddHttpClient<IExternalStockService, SmartTradeAdapter>(client =>
        {
            client.BaseAddress = new Uri(configuration["ExternalServices:SmartTradeUrl"] ?? "https://api.smarttrade.com");
        });

        return services;
    }
}