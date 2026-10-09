using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using StockAlert.Application.DTOs;
using StockAlert.Application.Interfaces;
using StockAlert.Domain.Entities;
using StockAlert.Domain.Enums;

namespace StockAlert.Infrastructure.Services;

public class ReportingService : IReportingService
{
    private const int LowStockThreshold = 5;
    private readonly IApplicationDbContext _context;

    public ReportingService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReportSummaryDto> GetSummaryAsync(
        DateTime? fromDate,
        DateTime? toDate)
    {
        ValidateDateRange(fromDate, toDate);

        var salesQuery = ApplyDateRange(
            _context.Sales.AsNoTracking(),
            sale => sale.SaleDate,
            fromDate,
            toDate);

        var movementsQuery = ApplyDateRange(
            _context.StockAdjustments.AsNoTracking(),
            movement => movement.CreatedAt,
            fromDate,
            toDate);

        var currentProducts = await _context.Products
            .AsNoTracking()
            .Where(product => !product.IsDeleted)
            .Select(product => new
            {
                product.Id,
                product.StockQuantity
            })
            .ToListAsync();

        var totalProducts = currentProducts.Count;

        var totalInventoryValue = await _context.Products
            .AsNoTracking()
            .Where(product => !product.IsDeleted)
            .Select(product => (decimal?)(product.Price * product.StockQuantity))
            .SumAsync() ?? 0m;

        var reservedByProduct = await _context.QuoteItems
            .AsNoTracking()
            .Where(item =>
                item.Quote != null
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

        var lowStockProducts = currentProducts.Count(product =>
            Math.Max(
                0,
                product.StockQuantity
                - reservedByProduct.GetValueOrDefault(product.Id))
            < LowStockThreshold);

        var totalSuppliers = await _context.Suppliers
            .AsNoTracking()
            .CountAsync();

        var saleCount = await salesQuery.CountAsync();

        var unitsSold = await salesQuery
            .Select(sale => (int?)sale.Quantity)
            .SumAsync() ?? 0;

        var salesRevenue = await salesQuery
            .Select(sale => (decimal?)sale.TotalPrice)
            .SumAsync() ?? 0m;

        var stockMovementCount = await movementsQuery.CountAsync();

        var unitsAdded = await movementsQuery
            .Where(movement => movement.QuantityChange > 0)
            .Select(movement => (int?)movement.QuantityChange)
            .SumAsync() ?? 0;

        var unitsRemovedRaw = await movementsQuery
            .Where(movement => movement.QuantityChange < 0)
            .Select(movement => (int?)movement.QuantityChange)
            .SumAsync() ?? 0;

        var topSellingProducts = await salesQuery
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

        var salespeople = await salesQuery
            .GroupBy(sale => sale.SalespersonName ?? "Unassigned")
            .Select(group => new SalespersonPerformanceDto(
                group.Key,
                group.Count(),
                group.Sum(sale => sale.Quantity),
                group.Sum(sale => sale.TotalPrice)
            ))
            .OrderByDescending(item => item.Revenue)
            .ThenByDescending(item => item.UnitsSold)
            .ToListAsync();

        return new ReportSummaryDto(
            FromDate: fromDate?.Date,
            ToDate: toDate?.Date,
            TotalProducts: totalProducts,
            TotalInventoryValue: totalInventoryValue,
            LowStockProducts: lowStockProducts,
            TotalSuppliers: totalSuppliers,
            SaleCount: saleCount,
            UnitsSold: unitsSold,
            SalesRevenue: salesRevenue,
            StockMovementCount: stockMovementCount,
            UnitsAdded: unitsAdded,
            UnitsRemoved: Math.Abs(unitsRemovedRaw),
            TopSellingProducts: topSellingProducts,
            Salespeople: salespeople
        );
    }

    public async Task<byte[]> GenerateSalesCsvAsync(
        DateTime? fromDate,
        DateTime? toDate)
    {
        ValidateDateRange(fromDate, toDate);

        var query = ApplyDateRange(
            _context.Sales
                .AsNoTracking()
                .Include(sale => sale.Product),
            sale => sale.SaleDate,
            fromDate,
            toDate);

        var sales = await query
            .OrderByDescending(sale => sale.SaleDate)
            .ToListAsync();

        var builder = new StringBuilder();
        builder.AppendLine("Date,Receipt,Salesperson,Product,Quantity,Unit Price,Total");

        foreach (var sale in sales)
        {
            var productName = sale.Product?.Name ?? "Unknown Product";
            var unitPrice = sale.Quantity > 0
                ? sale.TotalPrice / sale.Quantity
                : 0m;

            builder.AppendLine(string.Join(",",
                CsvCell(sale.SaleDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                CsvCell(sale.ReceiptNumber ?? string.Empty),
                CsvCell(sale.SalespersonName ?? "Unassigned"),
                CsvCell(productName),
                sale.Quantity.ToString(CultureInfo.InvariantCulture),
                unitPrice.ToString("0.00", CultureInfo.InvariantCulture),
                sale.TotalPrice.ToString("0.00", CultureInfo.InvariantCulture)));
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    public async Task<byte[]> GenerateStockMovementsCsvAsync(
        DateTime? fromDate,
        DateTime? toDate)
    {
        ValidateDateRange(fromDate, toDate);

        var query = ApplyDateRange(
            _context.StockAdjustments
                .AsNoTracking()
                .Include(movement => movement.Product),
            movement => movement.CreatedAt,
            fromDate,
            toDate);

        var movements = await query
            .OrderByDescending(movement => movement.CreatedAt)
            .ToListAsync();

        var builder = new StringBuilder();
        builder.AppendLine(
            "Date,Product,Previous Quantity,Quantity Change,New Quantity,Reason,Performed By");

        foreach (var movement in movements)
        {
            builder.AppendLine(string.Join(",",
                CsvCell(movement.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                CsvCell(movement.Product?.Name ?? "Unknown Product"),
                movement.PreviousQuantity.ToString(CultureInfo.InvariantCulture),
                movement.QuantityChange.ToString(CultureInfo.InvariantCulture),
                movement.NewQuantity.ToString(CultureInfo.InvariantCulture),
                CsvCell(movement.Reason),
                CsvCell(movement.PerformedBy)));
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    public async Task<byte[]> GenerateSuppliersCsvAsync()
    {
        var suppliers = await _context.Suppliers
            .AsNoTracking()
            .OrderBy(supplier => supplier.CompanyName)
            .ToListAsync();

        var products = await _context.Products
            .AsNoTracking()
            .Where(product => !product.IsDeleted)
            .Select(product => new
            {
                product.Id,
                product.SupplierId,
                product.StockQuantity
            })
            .ToListAsync();

        var productIds = products
            .Select(product => product.Id)
            .ToList();

        var reserved = await _context.QuoteItems
            .AsNoTracking()
            .Where(item =>
                productIds.Contains(item.ProductId)
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

        var builder = new StringBuilder();
        builder.AppendLine(
            "Supplier,Contact Email,Linked Products,Low Stock Products");

        foreach (var supplier in suppliers)
        {
            var supplierProducts = products
                .Where(product =>
                    product.SupplierId == supplier.Id)
                .ToList();

            var lowStockCount = supplierProducts.Count(product =>
                Math.Max(
                    0,
                    product.StockQuantity
                    - reserved.GetValueOrDefault(product.Id))
                < LowStockThreshold);

            builder.AppendLine(string.Join(",",
                CsvCell(supplier.CompanyName),
                CsvCell(supplier.ContactEmail ?? string.Empty),
                supplierProducts.Count.ToString(
                    CultureInfo.InvariantCulture),
                lowStockCount.ToString(
                    CultureInfo.InvariantCulture)));
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static IQueryable<T> ApplyDateRange<T>(
        IQueryable<T> query,
        System.Linq.Expressions.Expression<Func<T, DateTime>> dateSelector,
        DateTime? fromDate,
        DateTime? toDate)
    {
        if (fromDate.HasValue)
        {
            var from = fromDate.Value.Date;
            var parameter = dateSelector.Parameters[0];
            var condition = System.Linq.Expressions.Expression.GreaterThanOrEqual(
                dateSelector.Body,
                System.Linq.Expressions.Expression.Constant(from));
            var predicate =
                System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(
                    condition,
                    parameter);

            query = query.Where(predicate);
        }

        if (toDate.HasValue)
        {
            var toExclusive = toDate.Value.Date.AddDays(1);
            var parameter = dateSelector.Parameters[0];
            var condition = System.Linq.Expressions.Expression.LessThan(
                dateSelector.Body,
                System.Linq.Expressions.Expression.Constant(toExclusive));
            var predicate =
                System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(
                    condition,
                    parameter);

            query = query.Where(predicate);
        }

        return query;
    }

    private static void ValidateDateRange(DateTime? fromDate, DateTime? toDate)
    {
        if (fromDate.HasValue
            && toDate.HasValue
            && fromDate.Value.Date > toDate.Value.Date)
        {
            throw new ArgumentException(
                "From date cannot be later than to date.");
        }
    }

    private static string CsvCell(string value)
    {
        var safeValue = value;

        if (safeValue.StartsWith("=")
            || safeValue.StartsWith("+")
            || safeValue.StartsWith("-")
            || safeValue.StartsWith("@"))
        {
            safeValue = "'" + safeValue;
        }

        if (!safeValue.Contains(',')
            && !safeValue.Contains('"')
            && !safeValue.Contains('\n')
            && !safeValue.Contains('\r'))
        {
            return safeValue;
        }

        return $"\"{safeValue.Replace("\"", "\"\"")}\"";
    }
}
