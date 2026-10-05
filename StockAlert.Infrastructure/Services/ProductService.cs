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
                p.Description,
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

    public async Task<ProductDto?> GetProductByIdAsync(Guid id)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == id);

        return product == null ? null : ToDto(product);
    }

    public async Task<Guid> CreateProductAsync(CreateProductRequest request)
    {
        ValidateProductDetails(
            request.Name,
            request.Price,
            request.StockQuantity,
            request.CategoryName,
            request.SupplierName);

        var category = await GetOrCreateCategoryAsync(request.CategoryName);
        var supplier = await GetOrCreateSupplierAsync(
            request.SupplierName,
            request.SupplierEmail);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = NormalizeOptional(request.Description),
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            Category = category,
            Supplier = supplier
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync(default);

        return product.Id;
    }

    public async Task<bool> UpdateProductAsync(Guid id, UpdateProductRequest request)
    {
        ValidateProductDetails(
            request.Name,
            request.Price,
            0,
            request.CategoryName,
            request.SupplierName);

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return false;
        }

        var category = await GetOrCreateCategoryAsync(request.CategoryName);
        var supplier = await GetOrCreateSupplierAsync(
            request.SupplierName,
            request.SupplierEmail);

        product.Name = request.Name.Trim();
        product.Description = NormalizeOptional(request.Description);
        product.Price = request.Price;
        product.Category = category;
        product.Supplier = supplier;

        await _context.SaveChangesAsync(default);
        return true;
    }

    public async Task<bool> DeleteProductAsync(Guid id)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return false;
        }

        var hasSalesHistory = await _context.Sales
            .AsNoTracking()
            .AnyAsync(s => s.ProductId == id);

        var hasStockHistory = await _context.StockAdjustments
            .AsNoTracking()
            .AnyAsync(adjustment => adjustment.ProductId == id);

        if (hasSalesHistory || hasStockHistory)
        {
            throw new InvalidOperationException(
                "Products with sales or stock movement history cannot be deleted.");
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync(default);

        return true;
    }

    public async Task<bool> AdjustStockAsync(
        Guid id,
        AdjustStockRequest request,
        string performedBy)
    {
        if (request.QuantityChange == 0)
        {
            throw new ArgumentException("Quantity change cannot be zero.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("A stock adjustment reason is required.");
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return false;
        }

        var previousQuantity = product.StockQuantity;
        var newQuantity = previousQuantity + request.QuantityChange;

        if (newQuantity < 0)
        {
            throw new InvalidOperationException(
                "Stock adjustment cannot reduce quantity below zero.");
        }

        product.StockQuantity = newQuantity;

        _context.StockAdjustments.Add(new StockAdjustment
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            PreviousQuantity = previousQuantity,
            QuantityChange = request.QuantityChange,
            NewQuantity = newQuantity,
            Reason = request.Reason.Trim(),
            PerformedBy = string.IsNullOrWhiteSpace(performedBy)
                ? "Unknown User"
                : performedBy.Trim(),
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(default);
        return true;
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

        builder.AppendLine(
            "Product Name,Description,Category,Supplier,Price,Stock Quantity,Low Stock Alert");

        foreach (var product in products)
        {
            builder.AppendLine(
                $"{EscapeCsv(product.Name)}," +
                $"{EscapeCsv(product.Description ?? string.Empty)}," +
                $"{EscapeCsv(product.CategoryName)}," +
                $"{EscapeCsv(product.SupplierName)}," +
                $"{product.Price}," +
                $"{product.StockQuantity}," +
                $"{product.IsLowStock}");
        }

        return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
    }

    private async Task<Category> GetOrCreateCategoryAsync(string categoryName)
    {
        var normalizedName = categoryName.Trim();

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Name == normalizedName);

        if (category != null)
        {
            return category;
        }

        category = new Category
        {
            Id = Guid.NewGuid(),
            Name = normalizedName
        };

        _context.Categories.Add(category);
        return category;
    }

    private async Task<Supplier> GetOrCreateSupplierAsync(
        string supplierName,
        string? supplierEmail)
    {
        var normalizedName = supplierName.Trim();
        var normalizedEmail = NormalizeOptional(supplierEmail);

        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.CompanyName == normalizedName);

        if (supplier == null)
        {
            supplier = new Supplier
            {
                Id = Guid.NewGuid(),
                CompanyName = normalizedName,
                ContactEmail = normalizedEmail
            };

            _context.Suppliers.Add(supplier);
            return supplier;
        }

        if (normalizedEmail != null && supplier.ContactEmail != normalizedEmail)
        {
            supplier.ContactEmail = normalizedEmail;
        }

        return supplier;
    }

    private static ProductDto ToDto(Product product)
    {
        return new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.StockQuantity,
            product.Category?.Name ?? "N/A",
            product.StockQuantity < LowStockThreshold,
            product.Supplier?.CompanyName ?? "N/A",
            product.Supplier?.ContactEmail,
            product.ExternalId
        );
    }

    private static void ValidateProductDetails(
        string name,
        decimal price,
        int stockQuantity,
        string categoryName,
        string supplierName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.");
        }

        if (price <= 0)
        {
            throw new ArgumentException("Product price must be greater than zero.");
        }

        if (stockQuantity < 0)
        {
            throw new ArgumentException("Stock quantity cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(categoryName))
        {
            throw new ArgumentException("Category is required.");
        }

        if (string.IsNullOrWhiteSpace(supplierName))
        {
            throw new ArgumentException("Supplier is required.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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
