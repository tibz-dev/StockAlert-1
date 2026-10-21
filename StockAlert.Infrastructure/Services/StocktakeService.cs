using Microsoft.EntityFrameworkCore;
using StockAlert.Application.DTOs;
using StockAlert.Application.Interfaces;
using StockAlert.Domain.Entities;
using StockAlert.Domain.Enums;

namespace StockAlert.Infrastructure.Services;

public class StocktakeService : IStocktakeService
{
    private readonly IApplicationDbContext _context;
    private readonly IStockSourceAdapter _stockSourceAdapter;

    public StocktakeService(
        IApplicationDbContext context,
        IStockSourceAdapter stockSourceAdapter)
    {
        _context = context;
        _stockSourceAdapter = stockSourceAdapter;
    }

    public async Task<IReadOnlyList<StocktakeSessionDto>> GetAllAsync(
        bool canSeeExpected)
    {
        var sessions = await _context.StocktakeSessions
            .AsNoTracking()
            .Include(session => session.Items)
            .OrderByDescending(session => session.CreatedAt)
            .ToListAsync();

        return sessions
            .Select(session => ToSessionDto(session, canSeeExpected))
            .ToList();
    }

    public async Task<StocktakeSessionDto?> GetByIdAsync(
        Guid id,
        bool canSeeExpected)
    {
        var session = await _context.StocktakeSessions
            .AsNoTracking()
            .Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.Id == id);

        return session == null
            ? null
            : ToSessionDto(session, canSeeExpected);
    }

    public async Task<Guid> CreateAsync(
        CreateStocktakeRequest request,
        string createdBy)
    {
        var title = NormalizeRequired(
            request.Title,
            "Stocktake title is required.");

        var source = string.IsNullOrWhiteSpace(request.Source)
            ? "StockAlert"
            : request.Source.Trim();

        var session = new StocktakeSession
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = GenerateReferenceNumber(),
            Title = title,
            Location = NormalizeOptional(request.Location),
            Zone = NormalizeOptional(request.Zone),
            Source = source,
            BlindCount = request.BlindCount,
            Status = StocktakeStatus.Counting,
            CreatedBy = NormalizeActor(createdBy),
            CreatedAt = DateTime.UtcNow,
            SourceSnapshotAt = DateTime.UtcNow
        };

        if (source.Equals(
            "SmartTrade",
            StringComparison.OrdinalIgnoreCase))
        {
            await PopulateFromSmartTradeAsync(session);
        }
        else if (source.Equals(
            "StockAlert",
            StringComparison.OrdinalIgnoreCase))
        {
            await PopulateFromLocalCatalogueAsync(session);
        }
        else
        {
            throw new ArgumentException(
                "Stocktake source must be StockAlert or SmartTrade.");
        }

        if (session.Items.Count == 0)
        {
            throw new InvalidOperationException(
                $"{source} returned no products for this stocktake.");
        }

        _context.StocktakeSessions.Add(session);
        AddAudit(
            session.Id,
            "StocktakeStarted",
            createdBy,
            $"{session.ReferenceNumber} started from {source} with " +
            $"{session.Items.Count} product(s). Blind count: {session.BlindCount}.");

        await _context.SaveChangesAsync(default);
        return session.Id;
    }

    public async Task<StocktakeItemDto?> FindByBarcodeAsync(
        Guid sessionId,
        string barcode,
        bool canSeeExpected)
    {
        var normalized = NormalizeBarcode(barcode);

        var item = await _context.StocktakeItems
            .AsNoTracking()
            .Include(stocktakeItem => stocktakeItem.StocktakeSession)
            .FirstOrDefaultAsync(stocktakeItem =>
                stocktakeItem.StocktakeSessionId == sessionId
                && stocktakeItem.Barcode == normalized);

        if (item?.StocktakeSession == null)
        {
            return null;
        }

        return ToItemDto(
            item,
            item.StocktakeSession,
            canSeeExpected);
    }

    public async Task<StocktakeItemDto?> SubmitCountAsync(
        Guid sessionId,
        Guid itemId,
        SubmitStocktakeCountRequest request,
        string performedBy,
        bool canSeeExpected)
    {
        ValidateQuantity(request.Quantity);

        var item = await GetTrackedItemAsync(sessionId, itemId);

        if (item?.StocktakeSession == null)
        {
            return null;
        }

        EnsureSessionOpen(item.StocktakeSession);

        if (item.FirstCount.HasValue)
        {
            throw new InvalidOperationException(
                "The first count has already been submitted for this item.");
        }

        var actor = NormalizeActor(performedBy);
        var now = DateTime.UtcNow;

        item.FirstCount = request.Quantity;
        item.FirstCountedBy = actor;
        item.FirstCountedAt = now;

        if (request.Quantity == item.ExpectedQuantity)
        {
            item.FinalCount = request.Quantity;
            item.Status = StocktakeItemStatus.Matched;
        }
        else
        {
            item.FinalCount = null;
            item.Status = StocktakeItemStatus.RecountRequired;
        }

        AddAudit(
            item.Id,
            "CountSubmitted",
            actor,
            $"{item.ProductName}: first physical count = {request.Quantity}. " +
            $"{(item.Status == StocktakeItemStatus.Matched ? "Matched expected stock." : "Variance detected; recount required.")}");

        UpdateSessionStatus(item.StocktakeSession);
        await _context.SaveChangesAsync(default);

        return ToItemDto(
            item,
            item.StocktakeSession,
            canSeeExpected);
    }

    public async Task<StocktakeItemDto?> SubmitRecountAsync(
        Guid sessionId,
        Guid itemId,
        SubmitStocktakeCountRequest request,
        string performedBy,
        bool canSeeExpected)
    {
        ValidateQuantity(request.Quantity);

        var item = await GetTrackedItemAsync(sessionId, itemId);

        if (item?.StocktakeSession == null)
        {
            return null;
        }

        EnsureSessionOpen(item.StocktakeSession);

        if (item.Status != StocktakeItemStatus.RecountRequired
            || !item.FirstCount.HasValue)
        {
            throw new InvalidOperationException(
                "This item is not waiting for a recount.");
        }

        var actor = NormalizeActor(performedBy);

        if (string.Equals(
            item.FirstCountedBy,
            actor,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "A recount must be performed by a different staff member.");
        }

        item.SecondCount = request.Quantity;
        item.SecondCountedBy = actor;
        item.SecondCountedAt = DateTime.UtcNow;

        if (request.Quantity == item.ExpectedQuantity)
        {
            item.FinalCount = request.Quantity;
            item.Status = StocktakeItemStatus.Matched;
        }
        else
        {
            item.FinalCount = request.Quantity == item.FirstCount
                ? request.Quantity
                : null;
            item.Status = StocktakeItemStatus.NeedsReview;
        }

        var recountResult = item.Status == StocktakeItemStatus.Matched
            ? "Recount matched expected stock."
            : item.FinalCount.HasValue
                ? "Both counters confirmed the same variance; manager review required."
                : "First and second counts disagree; manager review required.";

        AddAudit(
            item.Id,
            "RecountSubmitted",
            actor,
            $"{item.ProductName}: second physical count = {request.Quantity}. {recountResult}");

        UpdateSessionStatus(item.StocktakeSession);
        await _context.SaveChangesAsync(default);

        return ToItemDto(
            item,
            item.StocktakeSession,
            canSeeExpected);
    }

    public async Task<StocktakeItemDto?> ResolveAsync(
        Guid sessionId,
        Guid itemId,
        ResolveStocktakeItemRequest request,
        string approvedBy)
    {
        ValidateQuantity(request.FinalQuantity);

        var reason = NormalizeRequired(
            request.Reason,
            "A reconciliation reason is required.");

        var item = await GetTrackedItemAsync(sessionId, itemId);

        if (item?.StocktakeSession == null)
        {
            return null;
        }

        EnsureSessionOpen(item.StocktakeSession);

        if (item.Status != StocktakeItemStatus.NeedsReview)
        {
            throw new InvalidOperationException(
                "Only items waiting for review can be resolved.");
        }

        var actor = NormalizeActor(approvedBy);

        item.FinalCount = request.FinalQuantity;
        item.ResolutionReason = reason;
        item.ApprovedBy = actor;
        item.ApprovedAt = DateTime.UtcNow;
        item.Status = StocktakeItemStatus.Approved;

        var variance = request.FinalQuantity - item.ExpectedQuantity;
        var value = variance * item.UnitValue;

        AddAudit(
            item.Id,
            "VarianceApproved",
            actor,
            $"{item.ProductName}: expected {item.ExpectedQuantity}, " +
            $"approved physical count {request.FinalQuantity}, " +
            $"variance {variance:+#;-#;0}, value {value:0.00}. Reason: {reason}");

        UpdateSessionStatus(item.StocktakeSession);
        await _context.SaveChangesAsync(default);

        return ToItemDto(
            item,
            item.StocktakeSession,
            true);
    }

    public async Task<bool> CompleteAsync(
        Guid sessionId,
        string completedBy)
    {
        var session = await _context.StocktakeSessions
            .Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.Id == sessionId);

        if (session == null)
        {
            return false;
        }

        EnsureSessionOpen(session);

        var unresolved = session.Items.Count(item =>
            item.Status == StocktakeItemStatus.Pending
            || item.Status == StocktakeItemStatus.RecountRequired
            || item.Status == StocktakeItemStatus.NeedsReview);

        if (unresolved > 0)
        {
            throw new InvalidOperationException(
                $"Stocktake cannot be completed while {unresolved} item(s) still require counting, recounting or review.");
        }

        var actor = NormalizeActor(completedBy);

        session.Status = StocktakeStatus.Completed;
        session.CompletedAt = DateTime.UtcNow;
        session.CompletedBy = actor;

        var varianceItems = session.Items.Count(item =>
            item.FinalCount.HasValue
            && item.FinalCount.Value != item.ExpectedQuantity);

        var varianceValue = session.Items
            .Where(item =>
                item.FinalCount.HasValue
                && item.FinalCount.Value != item.ExpectedQuantity)
            .Sum(item =>
                Math.Abs(
                    item.FinalCount!.Value
                    - item.ExpectedQuantity)
                * item.UnitValue);

        AddAudit(
            session.Id,
            "StocktakeCompleted",
            actor,
            $"{session.ReferenceNumber} completed. " +
            $"{varianceItems} variance item(s), " +
            $"variance exposure {varianceValue:0.00}. No stock quantity was automatically changed.");

        await _context.SaveChangesAsync(default);
        return true;
    }

    private async Task PopulateFromLocalCatalogueAsync(
        StocktakeSession session)
    {
        var products = await _context.Products
            .AsNoTracking()
            .Where(product => !product.IsDeleted)
            .OrderBy(product => product.Name)
            .ToListAsync();

        foreach (var product in products)
        {
            session.Items.Add(new StocktakeItem
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                ExternalProductId = product.ExternalId,
                ProductName = product.Name,
                Barcode = product.Barcode,
                UnitValue = product.Price,
                ExpectedQuantity = product.StockQuantity,
                Status = StocktakeItemStatus.Pending
            });
        }
    }

    private async Task PopulateFromSmartTradeAsync(
        StocktakeSession session)
    {
        var snapshot = await _stockSourceAdapter.GetSnapshotAsync();

        if (snapshot.Count == 0)
        {
            throw new InvalidOperationException(
                "SmartTrade returned no stock snapshot. The SmartTrade integration is not configured yet, so use StockAlert as the test source until API/CSV/database access is provided.");
        }

        var localProducts = await _context.Products
            .AsNoTracking()
            .Where(product => !product.IsDeleted)
            .ToListAsync();

        foreach (var sourceItem in snapshot)
        {
            var localProduct = FindLocalMatch(
                localProducts,
                sourceItem);

            session.Items.Add(new StocktakeItem
            {
                Id = Guid.NewGuid(),
                ProductId = localProduct?.Id,
                ExternalProductId = sourceItem.ExternalProductId,
                ProductName = sourceItem.ProductName,
                Barcode = NormalizeOptional(sourceItem.Barcode),
                UnitValue = sourceItem.UnitValue,
                ExpectedQuantity = sourceItem.QuantityOnHand,
                Status = StocktakeItemStatus.Pending
            });
        }
    }

    private async Task<StocktakeItem?> GetTrackedItemAsync(
        Guid sessionId,
        Guid itemId)
    {
        return await _context.StocktakeItems
            .Include(item => item.StocktakeSession)
                .ThenInclude(session => session!.Items)
            .FirstOrDefaultAsync(item =>
                item.Id == itemId
                && item.StocktakeSessionId == sessionId);
    }

    private void AddAudit(
        Guid entityId,
        string action,
        string user,
        string summary)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = "Stocktake",
            EntityId = entityId.ToString(),
            Action = action,
            UserId = NormalizeActor(user),
            Summary = summary,
            Timestamp = DateTime.UtcNow
        });
    }

    private static StocktakeSessionDto ToSessionDto(
        StocktakeSession session,
        bool canSeeExpected)
    {
        var items = session.Items
            .OrderBy(item => item.ProductName)
            .Select(item =>
                ToItemDto(item, session, canSeeExpected))
            .ToList();

        var countedItems = session.Items.Count(
            item => item.FirstCount.HasValue);

        var matchedItems = session.Items.Count(
            item => item.Status == StocktakeItemStatus.Matched);

        var varianceItems = session.Items.Count(item =>
        {
            var current = item.FinalCount ?? item.FirstCount;

            return current.HasValue
                && current.Value != item.ExpectedQuantity;
        });

        var varianceValue = session.Items.Sum(item =>
        {
            var current = item.FinalCount ?? item.FirstCount;

            if (!current.HasValue)
            {
                return 0m;
            }

            return Math.Abs(current.Value - item.ExpectedQuantity)
                * item.UnitValue;
        });

        return new StocktakeSessionDto(
            session.Id,
            session.ReferenceNumber,
            session.Title,
            session.Location,
            session.Zone,
            session.Source,
            session.Status.ToString(),
            session.BlindCount,
            session.CreatedBy,
            session.CreatedAt,
            session.SourceSnapshotAt,
            session.CompletedAt,
            session.CompletedBy,
            session.Items.Count,
            countedItems,
            matchedItems,
            varianceItems,
            session.Items.Count(item =>
                item.Status == StocktakeItemStatus.RecountRequired),
            session.Items.Count(item =>
                item.Status == StocktakeItemStatus.NeedsReview),
            varianceValue,
            items
        );
    }

    private static StocktakeItemDto ToItemDto(
        StocktakeItem item,
        StocktakeSession session,
        bool canSeeExpected)
    {
        var revealExpected =
            canSeeExpected
            || !session.BlindCount
            || item.FirstCount.HasValue;

        var currentCount = item.FinalCount ?? item.FirstCount;

        var variance = revealExpected && currentCount.HasValue
            ? currentCount.Value - item.ExpectedQuantity
            : (int?)null;

        var varianceValue = variance.HasValue
            ? variance.Value * item.UnitValue
            : (decimal?)null;

        return new StocktakeItemDto(
            item.Id,
            item.ProductId,
            item.ExternalProductId,
            item.ProductName,
            item.Barcode,
            item.UnitValue,
            revealExpected
                ? item.ExpectedQuantity
                : null,
            item.FirstCount,
            item.FirstCountedBy,
            item.FirstCountedAt,
            item.SecondCount,
            item.SecondCountedBy,
            item.SecondCountedAt,
            item.FinalCount,
            variance,
            varianceValue,
            item.Status.ToString(),
            item.ResolutionReason,
            item.ApprovedBy,
            item.ApprovedAt
        );
    }

    private static Product? FindLocalMatch(
        IReadOnlyCollection<Product> products,
        StockSourceItemDto sourceItem)
    {
        if (!string.IsNullOrWhiteSpace(sourceItem.ExternalProductId))
        {
            var byExternal = products.FirstOrDefault(product =>
                string.Equals(
                    product.ExternalId,
                    sourceItem.ExternalProductId,
                    StringComparison.OrdinalIgnoreCase));

            if (byExternal != null)
            {
                return byExternal;
            }
        }

        if (!string.IsNullOrWhiteSpace(sourceItem.Barcode))
        {
            var barcode = sourceItem.Barcode.Trim();

            var byBarcode = products.FirstOrDefault(product =>
                string.Equals(
                    product.Barcode,
                    barcode,
                    StringComparison.OrdinalIgnoreCase));

            if (byBarcode != null)
            {
                return byBarcode;
            }
        }

        return products.FirstOrDefault(product =>
            string.Equals(
                product.Name,
                sourceItem.ProductName,
                StringComparison.OrdinalIgnoreCase));
    }

    private static void UpdateSessionStatus(
        StocktakeSession session)
    {
        if (session.Status == StocktakeStatus.Completed
            || session.Status == StocktakeStatus.Cancelled)
        {
            return;
        }

        var hasPending = session.Items.Any(item =>
            item.Status == StocktakeItemStatus.Pending
            || item.Status == StocktakeItemStatus.RecountRequired);

        session.Status = hasPending
            ? StocktakeStatus.Counting
            : StocktakeStatus.Review;
    }

    private static void EnsureSessionOpen(
        StocktakeSession session)
    {
        if (session.Status == StocktakeStatus.Completed)
        {
            throw new InvalidOperationException(
                "This stocktake is already completed.");
        }

        if (session.Status == StocktakeStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "This stocktake is cancelled.");
        }
    }

    private static void ValidateQuantity(int quantity)
    {
        if (quantity < 0)
        {
            throw new ArgumentException(
                "Count quantity cannot be negative.");
        }
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

    private static string NormalizeBarcode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Barcode is required.");
        }

        return value.Trim();
    }

    private static string NormalizeActor(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "Unknown User"
            : value.Trim();
    }

    private static string GenerateReferenceNumber()
    {
        return $"STK-{DateTime.UtcNow:yyyyMMddHHmm}-{Guid.NewGuid():N}"[..25]
            .ToUpperInvariant();
    }
}
