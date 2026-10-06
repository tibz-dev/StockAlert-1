using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using StockAlert.Application.DTOs;
using StockAlert.Application.Interfaces;
using StockAlert.Domain.Entities;
using StockAlert.Domain.Enums;

namespace StockAlert.Infrastructure.Services;

public class DocumentDeliveryService : IDocumentDeliveryService
{
    private readonly IApplicationDbContext _context;
    private readonly IBusinessProfileService _businessProfileService;

    public DocumentDeliveryService(
        IApplicationDbContext context,
        IBusinessProfileService businessProfileService)
    {
        _context = context;
        _businessProfileService = businessProfileService;
    }

    public async Task<PreparedDeliveryDto> PrepareReceiptAsync(
        Guid saleId,
        PrepareDeliveryRequest request)
    {
        var sale = await _context.Sales
            .Include(item => item.Product)
            .Include(item => item.Customer)
            .FirstOrDefaultAsync(item => item.Id == saleId)
            ?? throw new KeyNotFoundException("Sale was not found.");

        var customer = sale.Customer
            ?? throw new InvalidOperationException(
                "This sale has no customer contact details.");

        if (string.IsNullOrWhiteSpace(sale.ReceiptNumber))
        {
            sale.ReceiptNumber = GenerateReceiptNumber();
            await _context.SaveChangesAsync(default);
        }

        var receiptSales = await _context.Sales
            .AsNoTracking()
            .Include(item => item.Product)
            .Where(item => item.ReceiptNumber == sale.ReceiptNumber)
            .OrderBy(item => item.SaleDate)
            .ToListAsync();

        var profile = await _businessProfileService.GetAsync();
        var channel = ParseChannel(request.Channel);
        var destination = GetDestination(customer, channel);

        var subject = $"Receipt {sale.ReceiptNumber} - {profile.BusinessName}";
        var body = BuildReceiptBody(profile, receiptSales, customer);
        var actionUrl = BuildActionUrl(channel, destination, subject, body);

        var log = new DeliveryLog
        {
            Id = Guid.NewGuid(),
            DocumentType = "Receipt",
            DocumentId = sale.Id,
            Channel = channel,
            Destination = destination,
            Status = DeliveryStatus.Prepared,
            ActionUrl = actionUrl,
            CreatedAt = DateTime.UtcNow
        };

        _context.DeliveryLogs.Add(log);
        await _context.SaveChangesAsync(default);

        return ToDto(log);
    }

    public async Task<PreparedDeliveryDto> PrepareQuoteAsync(
        Guid quoteId,
        PrepareDeliveryRequest request)
    {
        var quote = await _context.Quotes
            .Include(item => item.Customer)
            .Include(item => item.Items)
                .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(item => item.Id == quoteId)
            ?? throw new KeyNotFoundException("Quote was not found.");

        var customer = quote.Customer
            ?? throw new InvalidOperationException(
                "Quote customer details were not loaded.");

        var profile = await _businessProfileService.GetAsync();
        var channel = ParseChannel(request.Channel);
        var destination = GetDestination(customer, channel);

        var subject = $"Quote {quote.QuoteNumber} - {profile.BusinessName}";
        var body = BuildQuoteBody(profile, quote, customer);
        var actionUrl = BuildActionUrl(channel, destination, subject, body);

        if (quote.Status == QuoteStatus.Draft)
        {
            quote.Status = QuoteStatus.Sent;
            quote.SentAt = DateTime.UtcNow;
        }

        var log = new DeliveryLog
        {
            Id = Guid.NewGuid(),
            DocumentType = "Quote",
            DocumentId = quote.Id,
            Channel = channel,
            Destination = destination,
            Status = DeliveryStatus.Prepared,
            ActionUrl = actionUrl,
            CreatedAt = DateTime.UtcNow
        };

        _context.DeliveryLogs.Add(log);
        await _context.SaveChangesAsync(default);

        return ToDto(log);
    }

    private static string BuildReceiptBody(
        BusinessProfileDto profile,
        IReadOnlyList<Sale> sales,
        Customer customer)
    {
        if (sales.Count == 0)
        {
            throw new InvalidOperationException(
                "Receipt has no sale lines.");
        }

        var firstSale = sales[0];
        var receiptNumber = firstSale.ReceiptNumber ?? "Receipt";
        var builder = new StringBuilder();

        builder.AppendLine(profile.BusinessName);

        if (!string.IsNullOrWhiteSpace(profile.BranchName)
            || !string.IsNullOrWhiteSpace(profile.BranchNumber))
        {
            builder.AppendLine(
                $"Branch: {JoinNonEmpty(profile.BranchName, profile.BranchNumber)}");
        }

        var address = BuildAddress(profile);
        if (!string.IsNullOrWhiteSpace(address))
        {
            builder.AppendLine(address);
        }

        builder.AppendLine();
        builder.AppendLine($"RECEIPT: {receiptNumber}");
        builder.AppendLine(
            $"Date: {firstSale.SaleDate.ToLocalTime():yyyy-MM-dd HH:mm}");
        builder.AppendLine($"Customer: {customer.FullName}");
        builder.AppendLine();

        foreach (var sale in sales)
        {
            var unitPrice = sale.Quantity > 0
                ? sale.TotalPrice / sale.Quantity
                : 0m;

            builder.AppendLine(
                $"{sale.Product?.Name ?? "Product"} x {sale.Quantity} @ " +
                $"{profile.CurrencyCode} {unitPrice.ToString("0.00", CultureInfo.InvariantCulture)} = " +
                $"{profile.CurrencyCode} {sale.TotalPrice.ToString("0.00", CultureInfo.InvariantCulture)}");
        }

        builder.AppendLine();
        builder.AppendLine(
            $"TOTAL: {profile.CurrencyCode} " +
            $"{sales.Sum(item => item.TotalPrice).ToString("0.00", CultureInfo.InvariantCulture)}");

        if (!string.IsNullOrWhiteSpace(profile.ReceiptFooter))
        {
            builder.AppendLine();
            builder.AppendLine(profile.ReceiptFooter);
        }

        return builder.ToString().Trim();
    }

    private static string BuildQuoteBody(
        BusinessProfileDto profile,
        Quote quote,
        Customer customer)
    {
        var builder = new StringBuilder();

        builder.AppendLine(profile.BusinessName);

        if (!string.IsNullOrWhiteSpace(profile.RegistrationNumber))
        {
            builder.AppendLine(
                $"Registration: {profile.RegistrationNumber}");
        }

        if (!string.IsNullOrWhiteSpace(profile.VatNumber))
        {
            builder.AppendLine($"VAT: {profile.VatNumber}");
        }

        if (!string.IsNullOrWhiteSpace(profile.BranchName)
            || !string.IsNullOrWhiteSpace(profile.BranchNumber))
        {
            builder.AppendLine(
                $"Branch: {JoinNonEmpty(profile.BranchName, profile.BranchNumber)}");
        }

        var address = BuildAddress(profile);
        if (!string.IsNullOrWhiteSpace(address))
        {
            builder.AppendLine(address);
        }

        builder.AppendLine();
        builder.AppendLine($"QUOTE: {quote.QuoteNumber}");
        builder.AppendLine($"Customer: {customer.FullName}");

        if (!string.IsNullOrWhiteSpace(customer.CompanyName))
        {
            builder.AppendLine($"Company: {customer.CompanyName}");
        }

        builder.AppendLine($"Valid until: {quote.ValidUntil:yyyy-MM-dd}");
        builder.AppendLine();

        foreach (var item in quote.Items)
        {
            builder.AppendLine(
                $"{item.Product?.Name ?? "Product"} x {item.Quantity} @ " +
                $"{profile.CurrencyCode} {item.UnitPrice.ToString("0.00", CultureInfo.InvariantCulture)} = " +
                $"{profile.CurrencyCode} {item.LineTotal.ToString("0.00", CultureInfo.InvariantCulture)}");
        }

        builder.AppendLine();
        builder.AppendLine(
            $"Subtotal: {profile.CurrencyCode} {quote.Subtotal.ToString("0.00", CultureInfo.InvariantCulture)}");

        if (quote.VatAmount > 0)
        {
            builder.AppendLine(
                $"VAT ({quote.VatRate.ToString("0.##", CultureInfo.InvariantCulture)}%): " +
                $"{profile.CurrencyCode} {quote.VatAmount.ToString("0.00", CultureInfo.InvariantCulture)}");
        }

        builder.AppendLine(
            $"TOTAL: {profile.CurrencyCode} {quote.Total.ToString("0.00", CultureInfo.InvariantCulture)}");

        var bankDetails = BuildBankDetails(profile);
        if (!string.IsNullOrWhiteSpace(bankDetails))
        {
            builder.AppendLine();
            builder.AppendLine("Banking details:");
            builder.AppendLine(bankDetails);
        }

        if (!string.IsNullOrWhiteSpace(quote.Notes))
        {
            builder.AppendLine();
            builder.AppendLine($"Notes: {quote.Notes}");
        }

        return builder.ToString().Trim();
    }

    private static DeliveryChannel ParseChannel(string value)
    {
        if (!Enum.TryParse<DeliveryChannel>(
                value,
                ignoreCase: true,
                out var channel))
        {
            throw new ArgumentException(
                "Channel must be Email, Sms or WhatsApp.");
        }

        return channel;
    }

    private static string GetDestination(
        Customer customer,
        DeliveryChannel channel)
    {
        var destination = channel switch
        {
            DeliveryChannel.Email => customer.Email,
            DeliveryChannel.Sms => customer.PhoneNumber,
            DeliveryChannel.WhatsApp =>
                customer.HasWhatsApp
                    ? customer.WhatsAppNumber ?? customer.PhoneNumber
                    : customer.WhatsAppNumber,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(destination))
        {
            throw new InvalidOperationException(
                $"Customer does not have a valid {channel} destination.");
        }

        return destination.Trim();
    }

    private static string BuildActionUrl(
        DeliveryChannel channel,
        string destination,
        string subject,
        string body)
    {
        return channel switch
        {
            DeliveryChannel.Email =>
                $"mailto:{Uri.EscapeDataString(destination)}" +
                $"?subject={Uri.EscapeDataString(subject)}" +
                $"&body={Uri.EscapeDataString(body)}",

            DeliveryChannel.Sms =>
                $"sms:{NormalizePhone(destination)}" +
                $"?body={Uri.EscapeDataString(body)}",

            DeliveryChannel.WhatsApp =>
                $"https://wa.me/{NormalizeWhatsAppNumber(destination)}" +
                $"?text={Uri.EscapeDataString(body)}",

            _ => throw new ArgumentOutOfRangeException(nameof(channel))
        };
    }

    private static string BuildAddress(BusinessProfileDto profile)
    {
        return JoinNonEmpty(
            profile.AddressLine1,
            profile.AddressLine2,
            profile.City,
            profile.Province,
            profile.PostalCode,
            profile.Country);
    }

    private static string BuildBankDetails(BusinessProfileDto profile)
    {
        var lines = new[]
        {
            string.IsNullOrWhiteSpace(profile.BankName)
                ? null
                : $"Bank: {profile.BankName}",
            string.IsNullOrWhiteSpace(profile.BankAccountName)
                ? null
                : $"Account name: {profile.BankAccountName}",
            string.IsNullOrWhiteSpace(profile.BankAccountNumber)
                ? null
                : $"Account number: {profile.BankAccountNumber}",
            string.IsNullOrWhiteSpace(profile.BankBranchCode)
                ? null
                : $"Branch code: {profile.BankBranchCode}",
            string.IsNullOrWhiteSpace(profile.BankAccountType)
                ? null
                : $"Account type: {profile.BankAccountType}"
        };

        return string.Join(
            Environment.NewLine,
            lines.Where(line => line != null));
    }

    private static string JoinNonEmpty(params string?[] values)
    {
        return string.Join(
            ", ",
            values.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string NormalizePhone(string value)
    {
        var trimmed = value.Trim();
        var prefix = trimmed.StartsWith("+") ? "+" : string.Empty;
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());

        return prefix + digits;
    }

    private static string NormalizeWhatsAppNumber(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());

        if (digits.StartsWith("0") && digits.Length == 10)
        {
            return "27" + digits[1..];
        }

        return digits;
    }

    private static string GenerateReceiptNumber()
    {
        return $"RCPT-{DateTime.UtcNow:yyyyMMdd}-" +
               Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
    }

    private static PreparedDeliveryDto ToDto(DeliveryLog log)
    {
        return new PreparedDeliveryDto(
            log.Id,
            log.DocumentType,
            log.DocumentId,
            log.Channel.ToString(),
            log.Destination,
            log.Status.ToString(),
            log.ActionUrl ?? string.Empty
        );
    }
}
