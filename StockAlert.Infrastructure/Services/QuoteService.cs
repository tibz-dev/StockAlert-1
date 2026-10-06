using Microsoft.EntityFrameworkCore;
using StockAlert.Application.DTOs;
using StockAlert.Application.Interfaces;
using StockAlert.Domain.Entities;
using StockAlert.Domain.Enums;

namespace StockAlert.Infrastructure.Services;

public class QuoteService : IQuoteService
{
    private readonly IApplicationDbContext _context;
    private readonly IBusinessProfileService _businessProfileService;

    public QuoteService(
        IApplicationDbContext context,
        IBusinessProfileService businessProfileService)
    {
        _context = context;
        _businessProfileService = businessProfileService;
    }

    public async Task<IReadOnlyList<QuoteDto>> GetAllAsync()
    {
        var quotes = await _context.Quotes
            .AsNoTracking()
            .Include(quote => quote.Customer)
            .Include(quote => quote.Items)
                .ThenInclude(item => item.Product)
            .OrderByDescending(quote => quote.CreatedAt)
            .ToListAsync();

        var reservedByProduct = await GetReservedByProductAsync();

        return quotes
            .Select(quote => ToDto(quote, reservedByProduct))
            .ToList();
    }

    public async Task<QuoteDto?> GetByIdAsync(Guid id)
    {
        var quote = await _context.Quotes
            .AsNoTracking()
            .Include(item => item.Customer)
            .Include(item => item.Items)
                .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (quote == null)
        {
            return null;
        }

        var reservedByProduct = await GetReservedByProductAsync();
        return ToDto(quote, reservedByProduct);
    }

    public async Task<Guid> CreateAsync(
        CreateQuoteRequest request,
        string createdBy)
    {
        if (request.Items == null || request.Items.Count == 0)
        {
            throw new ArgumentException(
                "A quote must contain at least one product.");
        }

        if (string.IsNullOrWhiteSpace(request.CustomerName)
            && !request.CustomerId.HasValue)
        {
            throw new ArgumentException("Customer name is required.");
        }

        if (request.Items.Any(item => item.Quantity <= 0))
        {
            throw new ArgumentException(
                "All quote item quantities must be greater than zero.");
        }

        if (request.Items
            .GroupBy(item => item.ProductId)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "Each product can only appear once on a quote.");
        }

        var profile = await _businessProfileService.GetAsync();
        var customer = await ResolveCustomerAsync(request);

        var productIds = request.Items
            .Select(item => item.ProductId)
            .Distinct()
            .ToList();

        var products = await _context.Products
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id);

        if (products.Count != productIds.Count)
        {
            throw new ArgumentException(
                "One or more selected products no longer exist.");
        }

        var validUntil = request.ValidUntil?.Date
            ?? DateTime.UtcNow.Date.AddDays(profile.QuoteValidityDays);

        if (validUntil < DateTime.UtcNow.Date)
        {
            throw new ArgumentException(
                "Quote expiry date cannot be in the past.");
        }

        var quote = new Quote
        {
            Id = Guid.NewGuid(),
            QuoteNumber = GenerateQuoteNumber(),
            Customer = customer,
            Status = QuoteStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            ValidUntil = validUntil,
            Notes = Normalize(request.Notes),
            CreatedBy = Normalize(createdBy)
        };

        foreach (var requestItem in request.Items)
        {
            var product = products[requestItem.ProductId];
            var unitPrice = requestItem.UnitPrice ?? product.Price;

            if (unitPrice <= 0)
            {
                throw new ArgumentException(
                    $"Unit price for {product.Name} must be greater than zero.");
            }

            quote.Items.Add(new QuoteItem
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Product = product,
                Quantity = requestItem.Quantity,
                UnitPrice = unitPrice,
                LineTotal = unitPrice * requestItem.Quantity
            });
        }

        quote.Subtotal = quote.Items.Sum(item => item.LineTotal);
        quote.VatRate = profile.IsVatRegistered
            ? profile.DefaultVatRate
            : 0m;
        quote.VatAmount = decimal.Round(
            quote.Subtotal * quote.VatRate / 100m,
            2,
            MidpointRounding.AwayFromZero);
        quote.Total = quote.Subtotal + quote.VatAmount;

        _context.Quotes.Add(quote);
        await _context.SaveChangesAsync(default);

        return quote.Id;
    }

    public async Task<QuoteDto?> UpdateStatusAsync(
        Guid id,
        UpdateQuoteStatusRequest request)
    {
        if (!Enum.TryParse<QuoteStatus>(
                request.Status,
                ignoreCase: true,
                out var targetStatus))
        {
            throw new ArgumentException("Invalid quote status.");
        }

        var quote = await _context.Quotes
            .Include(item => item.Customer)
            .Include(item => item.Items)
                .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (quote == null)
        {
            return null;
        }

        if (quote.Status == QuoteStatus.Converted)
        {
            throw new InvalidOperationException(
                "Converted quotes can no longer be changed.");
        }

        if (targetStatus == QuoteStatus.Converted)
        {
            throw new InvalidOperationException(
                "Use the quote-to-sale conversion workflow to convert a quote.");
        }

        if (targetStatus == QuoteStatus.Accepted)
        {
            if (quote.ValidUntil.Date < DateTime.UtcNow.Date)
            {
                throw new InvalidOperationException(
                    "An expired quote cannot be accepted.");
            }

            foreach (var item in quote.Items)
            {
                var otherReserved = await _context.QuoteItems
                    .AsNoTracking()
                    .Where(other =>
                        other.ProductId == item.ProductId
                        && other.QuoteId != quote.Id
                        && other.Quote != null
                        && other.Quote.Status == QuoteStatus.Accepted)
                    .Select(other => (int?)other.Quantity)
                    .SumAsync() ?? 0;

                var onHand = item.Product?.StockQuantity ?? 0;
                var available = Math.Max(0, onHand - otherReserved);

                if (item.Quantity > available)
                {
                    throw new InvalidOperationException(
                        $"{item.Product?.Name ?? "Product"} only has " +
                        $"{available} unit(s) available after existing reservations.");
                }
            }

            quote.AcceptedAt = DateTime.UtcNow;
        }

        if (targetStatus == QuoteStatus.Sent && quote.SentAt == null)
        {
            quote.SentAt = DateTime.UtcNow;
        }

        quote.Status = targetStatus;
        await _context.SaveChangesAsync(default);

        var reservedByProduct = await GetReservedByProductAsync();
        return ToDto(quote, reservedByProduct);
    }

    public async Task<QuoteConversionDto?> ConvertToSaleAsync(Guid id)
    {
        var quote = await _context.Quotes
            .Include(item => item.Customer)
            .Include(item => item.Items)
                .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (quote == null)
        {
            return null;
        }

        if (quote.Status != QuoteStatus.Accepted)
        {
            throw new InvalidOperationException(
                "Only accepted quotes can be converted to a sale.");
        }

        if (quote.Customer == null)
        {
            throw new InvalidOperationException(
                "Quote customer details were not loaded.");
        }

        if (quote.Items.Count == 0)
        {
            throw new InvalidOperationException(
                "The quote has no items to convert.");
        }

        foreach (var item in quote.Items)
        {
            if (item.Product == null)
            {
                throw new InvalidOperationException(
                    "A quoted product no longer exists.");
            }

            if (item.Product.StockQuantity < item.Quantity)
            {
                throw new InvalidOperationException(
                    $"{item.Product.Name} no longer has enough physical stock to complete this quote.");
            }
        }

        var receiptNumber = GenerateReceiptNumber();
        var sales = new List<Sale>();

        foreach (var item in quote.Items)
        {
            var product = item.Product!;
            product.StockQuantity -= item.Quantity;

            var sale = new Sale
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Product = product,
                CustomerId = quote.CustomerId,
                Customer = quote.Customer,
                ReceiptNumber = receiptNumber,
                Quantity = item.Quantity,
                SaleDate = DateTime.UtcNow,
                TotalPrice = item.LineTotal
            };

            _context.Sales.Add(sale);
            sales.Add(sale);
        }

        quote.Status = QuoteStatus.Converted;
        quote.ConvertedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(default);

        var customer = quote.Customer;

        return new QuoteConversionDto(
            quote.Id,
            quote.QuoteNumber,
            sales[0].Id,
            receiptNumber,
            quote.Total,
            new CustomerDto(
                customer.Id,
                customer.FullName,
                customer.CompanyName,
                customer.Email,
                customer.PhoneNumber,
                customer.WhatsAppNumber,
                customer.HasWhatsApp,
                customer.Address
            )
        );
    }

    private async Task<Customer> ResolveCustomerAsync(
        CreateQuoteRequest request)
    {
        Customer? customer = null;

        if (request.CustomerId.HasValue)
        {
            customer = await _context.Customers
                .FirstOrDefaultAsync(item =>
                    item.Id == request.CustomerId.Value);

            if (customer == null)
            {
                throw new ArgumentException("Selected customer was not found.");
            }
        }

        var email = Normalize(request.CustomerEmail);
        var phone = Normalize(request.CustomerPhoneNumber);

        if (customer == null && email != null)
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
                FullName = request.CustomerName.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Customers.Add(customer);
        }

        if (!string.IsNullOrWhiteSpace(request.CustomerName))
        {
            customer.FullName = request.CustomerName.Trim();
        }

        customer.CompanyName = Normalize(request.CustomerCompanyName);
        customer.Email = email;
        customer.PhoneNumber = phone;
        customer.WhatsAppNumber = Normalize(
            request.CustomerWhatsAppNumber);
        customer.HasWhatsApp = request.CustomerHasWhatsApp;
        customer.Address = Normalize(request.CustomerAddress);

        return customer;
    }

    private async Task<Dictionary<Guid, int>> GetReservedByProductAsync()
    {
        return await _context.QuoteItems
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
    }

    private static QuoteDto ToDto(
        Quote quote,
        IReadOnlyDictionary<Guid, int> reservedByProduct)
    {
        var customer = quote.Customer
            ?? throw new InvalidOperationException(
                "Quote customer was not loaded.");

        var effectiveStatus =
            (quote.Status is QuoteStatus.Draft or QuoteStatus.Sent)
            && quote.ValidUntil.Date < DateTime.UtcNow.Date
                ? QuoteStatus.Expired
                : quote.Status;

        return new QuoteDto(
            quote.Id,
            quote.QuoteNumber,
            new CustomerDto(
                customer.Id,
                customer.FullName,
                customer.CompanyName,
                customer.Email,
                customer.PhoneNumber,
                customer.WhatsAppNumber,
                customer.HasWhatsApp,
                customer.Address
            ),
            effectiveStatus.ToString(),
            quote.CreatedAt,
            quote.ValidUntil,
            quote.SentAt,
            quote.AcceptedAt,
            quote.ConvertedAt,
            quote.Notes,
            quote.Subtotal,
            quote.VatRate,
            quote.VatAmount,
            quote.Total,
            quote.CreatedBy,
            quote.Items.Select(item =>
            {
                var product = item.Product;
                var reserved = reservedByProduct.GetValueOrDefault(
                    item.ProductId);
                var onHand = product?.StockQuantity ?? 0;

                return new QuoteItemDto(
                    item.Id,
                    item.ProductId,
                    product?.Name ?? "Unknown Product",
                    item.Quantity,
                    item.UnitPrice,
                    item.LineTotal,
                    onHand,
                    reserved,
                    Math.Max(0, onHand - reserved)
                );
            }).ToList()
        );
    }

    private static string GenerateReceiptNumber()
    {
        return $"RCPT-{DateTime.UtcNow:yyyyMMdd}-" +
               Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
    }

    private static string GenerateQuoteNumber()
    {
        return $"QT-{DateTime.UtcNow:yyyyMMdd}-" +
               Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
