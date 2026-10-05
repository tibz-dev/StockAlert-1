using Microsoft.EntityFrameworkCore;
using StockAlert.Application.DTOs;
using StockAlert.Application.Interfaces;
using StockAlert.Domain.Entities;

namespace StockAlert.Infrastructure.Services;

public class ProductService : IProductService
{
    private const int LowStockThreshold = 5;

    private readonly IApplicationDbContext _context;
    private readonly IExternalStockService _externalStockService;

    public ProductService(
        IApplicationDbContext context,
        IExternalStockService externalStockService)
    {
        _context = context;
        _externalStockService = externalStockService;
    }

    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .OrderBy(p => p.Name)
            .Select(p => new ProductDto(
                p.Id,
                p.Name,
                p.Price,
                p.StockQuantity,
                p.Category != null ? p.Category.Name : "N/A",
                p.StockQuantity < LowStockThreshold,
                p.Supplier != null ? p.Supplier.CompanyName : "N/A",
                p.Supplier != null ? p.Supplier.ContactEmail : null,
                p.ExternalId
            ))
            .ToListAsync();
    }

    public async Task<IEnumerable<SaleDto>> GetAllSalesAsync()
    {
        return await _context.Sales
            .AsNoTracking()
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
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == id);

        return product == null
            ? null
            : new ProductDto(
                product.Id,
                product.Name,
                product.Price,
                product.StockQuantity,
                product.Category?.Name ?? "N/A",
                product.StockQuantity < LowStockThreshold,
                product.Supplier?.CompanyName ?? "N/A",
                product.Supplier?.ContactEmail,
                product.ExternalId
            );
    }

    public async Task<Guid> CreateProductAsync(CreateProductRequest request)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Name == request.CategoryName);

        if (category == null)
        {
            category = new Category
            {
                Id = Guid.NewGuid(),
                Name = request.CategoryName
            };

            _context.Categories.Add(category);
        }

        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.CompanyName == request.SupplierName);

        if (supplier == null)
        {
            supplier = new Supplier
            {
                Id = Guid.NewGuid(),
                CompanyName = request.SupplierName,
                ContactEmail = request.SupplierEmail
            };

            _context.Suppliers.Add(supplier);
        }
        else if (!string.IsNullOrWhiteSpace(request.SupplierEmail)
                 && supplier.ContactEmail != request.SupplierEmail)
        {
            supplier.ContactEmail = request.SupplierEmail;
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
        if (request.Quantity <= 0)
        {
            return false;
        }

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
        var products = await _context.Products
            .AsNoTracking()
            .ToListAsync();

        var sales = await _context.Sales
            .AsNoTracking()
            .ToListAsync();

        return new DashboardDto(
            TotalProducts: products.Count,
            TotalInventoryValue: products.Sum(p => p.Price * p.StockQuantity),
            LowStockAlerts: products.Count(p => p.StockQuantity < LowStockThreshold),
            TotalSalesRevenue: sales.Sum(s => s.TotalPrice),
            TopSellingProducts: new List<ProductDto>(),
            DiscrepancyCount: 0,
            SyncLogs: new List<string>()
        );
    }

    public async Task<int> SyncWithSmartTradeAsync()
    {
        var externalProducts = await _externalStockService.SyncFromExternalAsync();
        var updateCount = 0;

        foreach (var externalProduct in externalProducts)
        {
            if (string.IsNullOrWhiteSpace(externalProduct.ExternalId))
            {
                continue;
            }

            var localProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.ExternalId == externalProduct.ExternalId);

            if (localProduct != null
                && localProduct.StockQuantity != externalProduct.StockQuantity)
            {
                localProduct.StockQuantity = externalProduct.StockQuantity;
                updateCount++;
            }
        }

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

        builder.AppendLine("Product Name,Category,Supplier,Price,Stock Quantity,Low Stock Alert");

        foreach (var product in products)
        {
            builder.AppendLine(
                $"{EscapeCsv(product.Name)}," +
                $"{EscapeCsv(product.CategoryName)}," +
                $"{EscapeCsv(product.SupplierName)}," +
                $"{product.Price}," +
                $"{product.StockQuantity}," +
                $"{product.IsLowStock}");
        }

        return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static string EscapeCsv(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
