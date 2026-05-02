using Microsoft.EntityFrameworkCore;
using StockAlert.Application.DTOs;
using StockAlert.Application.Interfaces;
using StockAlert.Domain.Entities;

namespace StockAlert.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly IApplicationDbContext _context;
    private readonly IExternalStockService _externalStockService;
    public ProductService(IApplicationDbContext context, IExternalStockService externalStockService)
    {
        _context = context;
        _externalStockService = externalStockService;
    }

    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
    {
        return await _context.Products
            .Include(p => p.Category)
            .Select(p => new ProductDto(
                p.Id,
                p.Name,
                p.Price,
                p.StockQuantity,
                p.Category != null ? p.Category.Name : "N/A",
                p.StockQuantity < 5,
                p.ExternalId
            ))
            .ToListAsync();
    }
    public async Task<IEnumerable<SaleDto>> GetAllSalesAsync()
    {
        return await _context.Sales
            .Include(s => s.Product)
            .OrderByDescending(s => s.SaleDate)
            .Select(s => new SaleDto(
                s.Id,
                s.Product != null ? s.Product.Name : "Unknown Product",
                s.Quantity,
                s.TotalPrice,
                s.SaleDate
            ))
            .ToListAsync();
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid id)
    {
        var p = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(x => x.Id == id);

        return p == null ? null : new ProductDto(p.Id, p.Name, p.Price, p.StockQuantity, p.Category?.Name ?? "N/A", p.StockQuantity < 5, p.ExternalId);
    }

    public async Task<Guid> CreateProductAsync(CreateProductRequest request)
    {
       
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Name == request.CategoryName);

        if (category == null)
        {
            category = new Category { Id = Guid.NewGuid(), Name = request.CategoryName };
            _context.Categories.Add(category);
        }

       
        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.CompanyName == request.SupplierName);

        if (supplier == null)
        {
            supplier = new Supplier { Id = Guid.NewGuid(), CompanyName = request.SupplierName };
            _context.Suppliers.Add(supplier);
        }

     
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            Category = category,
            Supplier = supplier
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync(default);

        return product.Id;
    }

    public async Task<bool> RecordSaleAsync(CreateSaleRequest request)
    {
        var product = await _context.Products.FindAsync(request.ProductId);


        if (product == null || product.StockQuantity < request.Quantity)
        {
            return false;
        }

       
        product.StockQuantity -= request.Quantity;

       
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Quantity = request.Quantity,
            SaleDate = DateTime.UtcNow,
            TotalPrice = product.Price * request.Quantity
        };

        _context.Sales.Add(sale);

       
        await _context.SaveChangesAsync(default);

        return true;
    }

    public async Task<DashboardDto> GetDashboardStatsAsync()
    {
        var products = await _context.Products.ToListAsync();
        var sales = await _context.Sales.ToListAsync();

        return new DashboardDto(
            TotalProducts: products.Count,
            TotalInventoryValue: products.Sum(p => p.Price * p.StockQuantity),
            LowStockAlerts: products.Count(p => p.StockQuantity < 5),
            TotalSalesRevenue: sales.Sum(s => s.TotalPrice),
            TopSellingProducts: (await GetAllProductsAsync()).ToList(),
            DiscrepancyCount: 0, // Placeholder
            SyncLogs: new List<string> { "Initial setup complete" } // Placeholder
        );
    }

    public async Task<int> SyncWithSmartTradeAsync()
    {
        // 1. Fetch products from SmartTrade (via our Adapter)
        var externalProducts = await _externalStockService.SyncFromExternalAsync();
        int updateCount = 0;

        foreach (var ext in externalProducts)
        {
            // 2. Find the local product using the ExternalId we added
            var localProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.ExternalId == ext.ExternalId);

            if (localProduct != null && localProduct.StockQuantity != ext.StockQuantity)
            {
                // 3. Update the local stock to match SmartTrade
                localProduct.StockQuantity = ext.StockQuantity;
                updateCount++;
            }
        }

        // 4. Save changes - This will trigger our AuditLog automatically!
        if (updateCount > 0)
        {
            await _context.SaveChangesAsync(default);
        }

        return updateCount;
    }

    public async Task<byte[]> GenerateStockReportAsync()
    {
        var products = await GetAllProductsAsync();
        var builder = new System.Text.StringBuilder();

        // Header row
        builder.AppendLine("Product Name,Category,Price,Stock Quantity,Low Stock Alert");

        foreach (var p in products)
        {
            builder.AppendLine($"{p.Name},{p.CategoryName},{p.Price},{p.StockQuantity},{p.IsLowStock}");
        }

        return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
    }
}