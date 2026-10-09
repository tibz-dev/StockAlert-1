using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockAlert.Application.Interfaces;
using StockAlert.Domain.Entities;
using StockAlert.Infrastructure.Persistence;
using StockAlert.Infrastructure.Services;

namespace StockAlert.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.AddScoped<AuthService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IStockMovementService, StockMovementService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddScoped<IBusinessProfileService, BusinessProfileService>();
        services.AddScoped<IQuoteService, QuoteService>();
        services.AddScoped<IDocumentDeliveryService, DocumentDeliveryService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IStaffService, StaffService>();
        services.AddScoped<IOutboundMessageSender, OutboundMessageSender>();
        services.AddScoped<IBarcodeLookupService, BarcodeLookupService>();

        services.AddHttpClient<IExternalStockService, SmartTradeAdapter>(client =>
        {
            client.BaseAddress = new Uri(
                configuration["ExternalServices:SmartTradeUrl"]
                ?? "https://api.smarttrade.com");
        });

        return services;
    }
}
