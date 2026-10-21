using Microsoft.EntityFrameworkCore;
using StockAlert.Application.DTOs;
using StockAlert.Application.Interfaces;
using StockAlert.Domain.Entities;
using StockAlert.Domain.Enums;

namespace StockAlert.Infrastructure.Services;

public class SupplierService : ISupplierService
{
    private const int LowStockThreshold = 5;

    private readonly IApplicationDbContext _context;

    public SupplierService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<SupplierDto>> GetAllAsync()
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
                product.Name,
                product.StockQuantity
            })
            .ToListAsync();

        var reserved = await _context.QuoteItems
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

        return suppliers
            .Select(supplier =>
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

                return new SupplierDto(
                    supplier.Id,
                    supplier.CompanyName,
                    supplier.ContactEmail,
                    supplierProducts.Count,
                    lowStockCount,
                    supplierProducts
                        .Select(product => product.Name)
                        .OrderBy(name => name)
                        .ToList()
                );
            })
            .ToList();
    }

    public async Task<SupplierDto?> GetByIdAsync(Guid id)
    {
        var suppliers = await GetAllAsync();
        return suppliers.FirstOrDefault(supplier => supplier.Id == id);
    }

    public async Task<IReadOnlyList<SupplierProductDto>> GetProductsAsync(
        Guid id)
    {
        var products = await _context.Products
            .AsNoTracking()
            .Where(product =>
                product.SupplierId == id
                && !product.IsDeleted)
            .OrderBy(product => product.Name)
            .ToListAsync();

        var productIds = products.Select(product => product.Id).ToList();

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

        return products.Select(product =>
        {
            var reservedQuantity = reserved.GetValueOrDefault(product.Id);
            var available = Math.Max(
                0,
                product.StockQuantity - reservedQuantity);

            return new SupplierProductDto(
                product.Id,
                product.Name,
                product.Barcode,
                product.Price,
                product.StockQuantity,
                reservedQuantity,
                available,
                available < LowStockThreshold
            );
        }).ToList();
    }

    public async Task<Guid> CreateAsync(CreateSupplierRequest request)
    {
        var companyName = NormalizeRequired(
            request.CompanyName,
            "Supplier company name is required.");

        var exists = await _context.Suppliers
            .AnyAsync(supplier => supplier.CompanyName == companyName);

        if (exists)
        {
            throw new InvalidOperationException(
                "A supplier with this company name already exists.");
        }

        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            CompanyName = companyName,
            ContactEmail = NormalizeOptional(request.ContactEmail)
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync(default);

        return supplier.Id;
    }

    public async Task<bool> UpdateAsync(
        Guid id,
        UpdateSupplierRequest request)
    {
        var companyName = NormalizeRequired(
            request.CompanyName,
            "Supplier company name is required.");

        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(item => item.Id == id);

        if (supplier == null)
        {
            return false;
        }

        var duplicateExists = await _context.Suppliers
            .AnyAsync(item =>
                item.Id != id
                && item.CompanyName == companyName);

        if (duplicateExists)
        {
            throw new InvalidOperationException(
                "Another supplier already uses this company name.");
        }

        supplier.CompanyName = companyName;
        supplier.ContactEmail = NormalizeOptional(request.ContactEmail);

        await _context.SaveChangesAsync(default);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(item => item.Id == id);

        if (supplier == null)
        {
            return false;
        }

        var hasProducts = await _context.Products
            .AsNoTracking()
            .AnyAsync(product => product.SupplierId == id);

        if (hasProducts)
        {
            throw new InvalidOperationException(
                "Suppliers linked to products cannot be deleted.");
        }

        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync(default);

        return true;
    }

    private static string NormalizeRequired(
        string value,
        string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
