using System.Data;
using Microsoft.EntityFrameworkCore;
using StockAlert.Application.DTOs;
using StockAlert.Domain.Entities;
using StockAlert.Domain.Enums;
using StockAlert.Infrastructure.Persistence;

namespace StockAlert.Infrastructure.Services;

/// <summary>
/// Posts a fully confirmed cash checkout atomically. Other tender types must
/// use a verified payment integration before a posting path is introduced.
/// </summary>
public sealed class CheckoutPostingService
{
    private readonly ApplicationDbContext _db;
    public CheckoutPostingService(ApplicationDbContext db) => _db = db;

    public async Task<CheckoutReceiptDto> CommitCashAsync(
        CommitCheckoutRequest request, CancellationToken cancellationToken)
    {
        if (request.ClientOperationId == Guid.Empty)
            throw new ArgumentException("ClientOperationId is required for retry safety.");
        if (request.ExpectedTotal <= 0)
            throw new ArgumentException("Expected cash total must be positive.");
        if (!request.CashReceived)
            throw new ArgumentException("Confirm cash was physically received before checkout.");
        if (request.Items is not { Count: > 0 and <= 100 } ||
            request.Items.Any(x => x.ProductId == Guid.Empty || x.Quantity <= 0) ||
            request.Items.Select(x => x.ProductId).Distinct().Count() != request.Items.Count)
            throw new ArgumentException("Supply 1 to 100 distinct products with positive quantities.");

        // Serializable makes stock and reservations checked within the same transaction.
        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        var previous = await _db.CheckoutTransactions.AsNoTracking()
            .Include(x => x.Lines).ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.ClientOperationId == request.ClientOperationId, cancellationToken);
        if (previous != null)
        {
            var sameLines = previous.Lines.Count == request.Items.Count &&
                request.Items.All(item => previous.Lines.Any(existing =>
                    existing.ProductId == item.ProductId && existing.Quantity == item.Quantity));
            if (!sameLines || previous.CustomerId != request.CustomerId || previous.Total != request.ExpectedTotal)
                throw new InvalidOperationException(
                    "This operation ID belongs to a different checkout request.");
            await transaction.CommitAsync(cancellationToken);
            return ToReceipt(previous);
        }

        var requestedIds = request.Items.Select(x => x.ProductId).ToArray();
        var products = await _db.Products
            .Where(x => requestedIds.Contains(x.Id) && !x.IsDeleted)
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        if (products.Count != requestedIds.Length)
            throw new InvalidOperationException("One or more products are unavailable.");

        if (request.CustomerId.HasValue &&
            !await _db.Customers.AnyAsync(x => x.Id == request.CustomerId, cancellationToken))
            throw new ArgumentException("Customer does not exist.");

        var reserved = await _db.QuoteItems.AsNoTracking()
            .Where(x => requestedIds.Contains(x.ProductId) &&
                x.Quote != null && x.Quote.Status == QuoteStatus.Accepted)
            .GroupBy(x => x.ProductId)
            .Select(g => new { Id = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.Id, x => x.Quantity, cancellationToken);

        var header = new CheckoutTransaction
        {
            Id = Guid.NewGuid(),
            ClientOperationId = request.ClientOperationId,
            ReceiptNumber = "POS-" + Guid.NewGuid().ToString("N").ToUpperInvariant(),
            CustomerId = request.CustomerId,
            Status = "Posted",
            CreatedAtUtc = DateTime.UtcNow
        };

        foreach (var line in request.Items)
        {
            var product = products[line.ProductId];
            var available = (long)product.StockQuantity - reserved.GetValueOrDefault(product.Id);
            if (available < line.Quantity)
                throw new InvalidOperationException($"Insufficient unreserved stock for {product.Name}.");

            decimal amount;
            try { amount = checked(decimal.Round(product.Price * line.Quantity, 2)); }
            catch (OverflowException) { throw new ArgumentException("Checkout amount exceeds supported limits."); }
            if (product.Price <= 0 || amount <= 0)
                throw new InvalidOperationException($"Invalid sale price for {product.Name}.");

            product.StockQuantity -= line.Quantity;
            header.Lines.Add(new CheckoutLine
            {
                Id = Guid.NewGuid(), ProductId = product.Id, Quantity = line.Quantity,
                UnitPrice = product.Price, LineTotal = amount
            });
        }

        try { header.Subtotal = checked(header.Lines.Sum(x => x.LineTotal)); }
        catch (OverflowException) { throw new ArgumentException("Checkout total exceeds supported limits."); }
        var business = await _db.BusinessProfiles.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var vatRate = business?.IsVatRegistered == true ? business.DefaultVatRate : 0m;
        if (vatRate < 0 || vatRate > 100)
            throw new InvalidOperationException("Invalid business VAT rate.");

        // The product-price policy is currently the same as quote creation:
        // catalogue prices are exclusive of VAT when business VAT is enabled.
        header.VatAmount = decimal.Round(header.Subtotal * vatRate / 100m, 2,
            MidpointRounding.AwayFromZero);
        header.Total = checked(header.Subtotal + header.VatAmount);
        if (header.Total != request.ExpectedTotal)
            throw new InvalidOperationException("Checkout total changed. Preview again before taking payment.");
        header.Tenders.Add(new CheckoutTender
        {
            Id = Guid.NewGuid(), Method = "Cash", Amount = header.Total,
            Status = "Received"
        });

        _db.CheckoutTransactions.Add(header);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToReceipt(header);
    }

    private static CheckoutReceiptDto ToReceipt(CheckoutTransaction checkout) =>
        new(checkout.Id, checkout.ReceiptNumber, checkout.Status,
            checkout.Subtotal, checkout.VatAmount, checkout.Total, checkout.CreatedAtUtc,
            checkout.Lines.Select(x => new CheckoutPreviewLineDto(
                x.ProductId, x.Product?.Name ?? "Product", x.Product?.Barcode,
                x.Quantity, x.UnitPrice, x.LineTotal, 0)).ToList());
}
