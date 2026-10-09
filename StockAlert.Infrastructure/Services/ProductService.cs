using Microsoft.EntityFrameworkCore;
using StockAlert.Application.DTOs;
using StockAlert.Application.Interfaces;
using StockAlert.Domain.Entities;
using StockAlert.Domain.Enums;

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
        var products = await _context.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.Supplier)
            .OrderBy(product => product.Name)
            .ToListAsync();

        var metrics = await GetProductMetricsAsync(
            products.Select(product => product.Id));

        return products.Select(product =>
            ToDto(
                product,
                metrics.GetValueOrDefault(
                    product.Id,
                    ProductMetrics.Empty)));
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid id)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(item => item.Category)
            .Include(item => item.Supplier)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (product == null)
        {
            return null;
        }

        var metrics = await GetProductMetricsAsync(new[] { id });

        return ToDto(
            product,
            metrics.GetValueOrDefault(id, ProductMetrics.Empty));
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

        var hasQuoteHistory = await _context.QuoteItems
            .AsNoTracking()
            .AnyAsync(item => item.ProductId == id);

        if (hasSalesHistory || hasStockHistory || hasQuoteHistory)
        {
            throw new InvalidOperationException(
                "Products with sales, stock movement, or quote history cannot be deleted.");
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

        var reservedQuantity = await GetReservedQuantityAsync(product.Id);

        if (newQuantity < reservedQuantity)
        {
            throw new InvalidOperationException(
                $"Stock cannot be reduced below the {reservedQuantity} unit(s) reserved by accepted quotes.");
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

    public async Task<SaleReceiptDto?> RecordSaleAsync(
        CreateSaleRequest request)
    {
        if (request.Quantity <= 0)
        {
            return null;
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(item => item.Id == request.ProductId);

        if (product == null)
        {
            return null;
        }

        var reservedQuantity = await GetReservedQuantityAsync(product.Id);
        var availableQuantity = Math.Max(
            0,
            product.StockQuantity - reservedQuantity);

        if (request.Quantity > availableQuantity)
        {
            throw new InvalidOperationException(
                $"Only {availableQuantity} unreserved unit(s) of {product.Name} are available.");
        }

        var customer = await ResolveSaleCustomerAsync(request);

        product.StockQuantity -= request.Quantity;

        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Product = product,
            Customer = customer,
            ReceiptNumber = GenerateReceiptNumber(),
            Quantity = request.Quantity,
            SaleDate = DateTime.UtcNow,
            TotalPrice = product.Price * request.Quantity
        };

        _context.Sales.Add(sale);
        await _context.SaveChangesAsync(default);

        return new SaleReceiptDto(
            sale.Id,
            sale.ReceiptNumber,
            product.Name,
            sale.Quantity,
            product.Price,
            sale.TotalPrice,
            sale.SaleDate,
            customer == null
                ? null
                : ToCustomerDto(customer)
        );
    }

    public async Task<DashboardDto> GetDashboardStatsAsync()
    {
        var totalProducts = await _context.Products
            .AsNoTracking()
            .CountAsync();

        var totalInventoryValue = await _context.Products
            .AsNoTracking()
            .Select(product => (decimal?)(product.Price * product.StockQuantity))
            .SumAsync() ?? 0m;

        var dashboardProducts = await _context.Products
            .AsNoTracking()
            .Select(product => new
            {
                product.Id,
                product.SupplierId,
                product.StockQuantity
            })
            .ToListAsync();

        var dashboardMetrics = await GetProductMetricsAsync(
            dashboardProducts.Select(product => product.Id));

        var lowStockProducts = dashboardProducts
            .Where(product =>
            {
                var reserved = dashboardMetrics
                    .GetValueOrDefault(
                        product.Id,
                        ProductMetrics.Empty)
                    .ReservedQuantity;

                return Math.Max(
                    0,
                    product.StockQuantity - reserved)
                    < LowStockThreshold;
            })
            .ToList();

        var lowStockAlerts = lowStockProducts.Count;

        var totalSalesRevenue = await _context.Sales
            .AsNoTracking()
            .Select(sale => (decimal?)sale.TotalPrice)
            .SumAsync() ?? 0m;

        var totalUnitsSold = await _context.Sales
            .AsNoTracking()
            .Select(sale => (int?)sale.Quantity)
            .SumAsync() ?? 0;

        var totalSuppliers = await _context.Suppliers
            .AsNoTracking()
            .CountAsync();

        var totalCustomers = await _context.Customers
            .AsNoTracking()
            .CountAsync();

        var today = DateTime.UtcNow.Date;

        var activeQuotes = await _context.Quotes
            .AsNoTracking()
            .Where(quote =>
                quote.Status == QuoteStatus.Accepted
                || ((quote.Status == QuoteStatus.Draft
                    || quote.Status == QuoteStatus.Sent)
                    && quote.ValidUntil >= today))
            .Include(quote => quote.Payments)
            .ToListAsync();

        var activeQuoteCount = activeQuotes.Count;
        var quotePipelineValue = activeQuotes.Sum(quote => quote.Total);
        var outstandingQuoteBalance = activeQuotes.Sum(quote =>
            Math.Max(
                0m,
                quote.Total - quote.Payments.Sum(payment => payment.Amount)));

        var reservedUnits = await _context.QuoteItems
            .AsNoTracking()
            .Where(item =>
                item.Quote != null
                && item.Quote.Status == QuoteStatus.Accepted)
            .Select(item => (int?)item.Quantity)
            .SumAsync() ?? 0;

        var topSellingProducts = await _context.Sales
            .AsNoTracking()
            .GroupBy(sale => sale.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                UnitsSold = group.Sum(sale => sale.Quantity),
                Revenue = group.Sum(sale => sale.TotalPrice)
            })
            .OrderByDescending(item => item.UnitsSold)
            .ThenByDescending(item => item.Revenue)
            .Take(5)
            .Join(
                _context.Products.AsNoTracking(),
                salesSummary => salesSummary.ProductId,
                product => product.Id,
                (salesSummary, product) => new TopSellingProductDto(
                    product.Id,
                    product.Name,
                    salesSummary.UnitsSold,
                    salesSummary.Revenue,
                    product.StockQuantity
                ))
            .ToListAsync();

        var recentSales = await _context.Sales
            .AsNoTracking()
            .Include(sale => sale.Product)
            .OrderByDescending(sale => sale.SaleDate)
            .Take(5)
            .Select(sale => new SaleDto(
                sale.Id,
                sale.Product != null
                    ? sale.Product.Name
                    : "Unknown Product",
                sale.Quantity,
                sale.TotalPrice,
                sale.SaleDate
            ))
            .ToListAsync();

        var recentStockMovements = await _context.StockAdjustments
            .AsNoTracking()
            .Include(adjustment => adjustment.Product)
            .OrderByDescending(adjustment => adjustment.CreatedAt)
            .Take(5)
            .Select(adjustment => new StockAdjustmentDto(
                adjustment.Id,
                adjustment.ProductId,
                adjustment.Product != null
                    ? adjustment.Product.Name
                    : "Unknown Product",
                adjustment.PreviousQuantity,
                adjustment.QuantityChange,
                adjustment.NewQuantity,
                adjustment.Reason,
                adjustment.PerformedBy,
                adjustment.CreatedAt
            ))
            .ToListAsync();

        var supplierLowStockCounts = lowStockProducts
            .GroupBy(product => product.SupplierId)
            .ToDictionary(
                group => group.Key,
                group => group.Count());

        var supplierIds = supplierLowStockCounts.Keys.ToList();

        var supplierLowStockDetails = await _context.Suppliers
            .AsNoTracking()
            .Where(supplier => supplierIds.Contains(supplier.Id))
            .ToListAsync();

        var lowStockSupplierAlerts = supplierLowStockDetails
            .Select(supplier => new LowStockSupplierAlertDto(
                supplier.Id,
                supplier.CompanyName,
                supplier.ContactEmail,
                supplierLowStockCounts.GetValueOrDefault(supplier.Id)
            ))
            .OrderByDescending(alert => alert.LowStockProductCount)
            .ToList();

        return new DashboardDto(
            TotalProducts: totalProducts,
            TotalInventoryValue: totalInventoryValue,
            LowStockAlerts: lowStockAlerts,
            TotalSalesRevenue: totalSalesRevenue,
            TotalUnitsSold: totalUnitsSold,
            TotalSuppliers: totalSuppliers,
            TotalCustomers: totalCustomers,
            ActiveQuotes: activeQuoteCount,
            QuotePipelineValue: quotePipelineValue,
            OutstandingQuoteBalance: outstandingQuoteBalance,
            ReservedUnits: reservedUnits,
            TopSellingProducts: topSellingProducts,
            RecentSales: recentSales,
            RecentStockMovements: recentStockMovements,
            LowStockSupplierAlerts: lowStockSupplierAlerts,
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
            "Product Name,Description,Category,Supplier,Price,On Hand,Under Quote,Reserved,Available,Sold,Low Stock Alert");

        foreach (var product in products)
        {
            builder.AppendLine(
                $"{EscapeCsv(product.Name)}," +
                $"{EscapeCsv(product.Description ?? string.Empty)}," +
                $"{EscapeCsv(product.CategoryName)}," +
                $"{EscapeCsv(product.SupplierName)}," +
                $"{product.Price}," +
                $"{product.StockQuantity}," +
                $"{product.QuotedQuantity}," +
                $"{product.ReservedQuantity}," +
                $"{product.AvailableQuantity}," +
                $"{product.SoldQuantity}," +
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

    private static ProductDto ToDto(
        Product product,
        ProductMetrics metrics)
    {
        var availableQuantity = Math.Max(
            0,
            product.StockQuantity - metrics.ReservedQuantity);

        return new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.StockQuantity,
            product.Category?.Name ?? "N/A",
            availableQuantity < LowStockThreshold,
            product.Supplier?.CompanyName ?? "N/A",
            product.Supplier?.ContactEmail,
            product.ExternalId,
            metrics.QuotedQuantity,
            metrics.ReservedQuantity,
            availableQuantity,
            metrics.SoldQuantity
        );
    }

    private async Task<Dictionary<Guid, ProductMetrics>>
        GetProductMetricsAsync(IEnumerable<Guid> productIds)
    {
        var ids = productIds.Distinct().ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<Guid, ProductMetrics>();
        }

        var today = DateTime.UtcNow.Date;

        var quoted = await _context.QuoteItems
            .AsNoTracking()
            .Where(item =>
                ids.Contains(item.ProductId)
                && item.Quote != null
                && (item.Quote.Status == QuoteStatus.Sent
                    || item.Quote.Status == QuoteStatus.Accepted)
                && (item.Quote.Status == QuoteStatus.Accepted
                    || item.Quote.ValidUntil >= today))
            .GroupBy(item => item.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                Quantity = group.Sum(item => item.Quantity)
            })
            .ToDictionaryAsync(
                item => item.ProductId,
                item => item.Quantity);

        var reserved = await _context.QuoteItems
            .AsNoTracking()
            .Where(item =>
                ids.Contains(item.ProductId)
                && item.Quote != null
                && item.Quote.Status == QuoteStatus.Accepted)
            .GroupBy(item => item.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                Quantity = group.Sum(item => item.Quantity)
            })
            .ToDictionaryAsync(
                item => item.ProductId,
                item => item.Quantity);

        var sold = await _context.Sales
            .AsNoTracking()
            .Where(sale => ids.Contains(sale.ProductId))
            .GroupBy(sale => sale.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                Quantity = group.Sum(sale => sale.Quantity)
            })
            .ToDictionaryAsync(
                item => item.ProductId,
                item => item.Quantity);

        return ids.ToDictionary(
            id => id,
            id => new ProductMetrics(
                quoted.GetValueOrDefault(id),
                reserved.GetValueOrDefault(id),
                sold.GetValueOrDefault(id)));
    }

    private async Task<int> GetReservedQuantityAsync(Guid productId)
    {
        return await _context.QuoteItems
            .AsNoTracking()
            .Where(item =>
                item.ProductId == productId
                && item.Quote != null
                && item.Quote.Status == QuoteStatus.Accepted)
            .Select(item => (int?)item.Quantity)
            .SumAsync() ?? 0;
    }

    private async Task<Customer?> ResolveSaleCustomerAsync(
        CreateSaleRequest request)
    {
        var name = NormalizeOptional(request.CustomerName);
        var email = NormalizeOptional(request.CustomerEmail);
        var phone = NormalizeOptional(request.CustomerPhoneNumber);
        var whatsApp = NormalizeOptional(request.CustomerWhatsAppNumber);

        if (name == null
            && email == null
            && phone == null
            && whatsApp == null)
        {
            return null;
        }

        Customer? customer = null;

        if (email != null)
        {
            customer = await _context.Customers
                .FirstOrDefaultAsync(item => item.Email == email);
        }

        if (customer == null && phone != null)
        {
            customer = await _context.Customers
                .FirstOrDefaultAsync(item => item.PhoneNumber == phone);
        }

        if (customer == null)
        {
            customer = new Customer
            {
                Id = Guid.NewGuid(),
                FullName = name
                    ?? email
                    ?? phone
                    ?? "Walk-in Customer",
                CreatedAt = DateTime.UtcNow
            };

            _context.Customers.Add(customer);
        }

        if (name != null)
        {
            customer.FullName = name;
        }

        customer.CompanyName = NormalizeOptional(
            request.CustomerCompanyName);
        customer.Email = email;
        customer.PhoneNumber = phone;
        customer.WhatsAppNumber = whatsApp;
        customer.HasWhatsApp = request.CustomerHasWhatsApp;
        customer.Address = NormalizeOptional(request.CustomerAddress);

        return customer;
    }

    private static CustomerDto ToCustomerDto(Customer customer)
    {
        return new CustomerDto(
            customer.Id,
            customer.FullName,
            customer.CompanyName,
            customer.Email,
            customer.PhoneNumber,
            customer.WhatsAppNumber,
            customer.HasWhatsApp,
            customer.Address
        );
    }

    private static string GenerateReceiptNumber()
    {
        return $"RCPT-{DateTime.UtcNow:yyyyMMdd}-" +
               Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
    }

    private sealed record ProductMetrics(
        int QuotedQuantity,
        int ReservedQuantity,
        int SoldQuantity)
    {
        public static ProductMetrics Empty { get; } = new(0, 0, 0);
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
