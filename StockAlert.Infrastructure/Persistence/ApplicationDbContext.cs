using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StockAlert.Application.Interfaces;
using StockAlert.Domain.Entities;
using StockAlert.Domain.Enums;
using System.Security.Claims;
using System.Text.Json;

namespace StockAlert.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IHttpContextAccessor? httpContextAccessor = null) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<BusinessProfile> BusinessProfiles => Set<BusinessProfile>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<QuoteItem> QuoteItems => Set<QuoteItem>();
    public DbSet<DeliveryLog> DeliveryLogs => Set<DeliveryLog>();
    public DbSet<QuotePayment> QuotePayments => Set<QuotePayment>();
    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        OnBeforeSaveChanges();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void OnBeforeSaveChanges()
    {
        ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditLog>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog
                || entry.Entity is ApplicationUser
                || entry.Entity is Customer
                || entry.Entity is DeliveryLog
                || IsIdentityEntity(entry.Entity.GetType())
                || entry.State == EntityState.Detached
                || entry.State == EntityState.Unchanged)
            {
                continue;
            }

            var action = GetAuditAction(entry);
            var auditEntry = new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityName = entry.Entity.GetType().Name,
                EntityId = GetEntityId(entry),
                Action = action,
                UserId = GetCurrentUserIdentifier(),
                Summary = GetAuditSummary(entry, action),
                IpAddress = _httpContextAccessor?
                    .HttpContext?
                    .Connection
                    .RemoteIpAddress?
                    .ToString(),
                Timestamp = DateTime.UtcNow,
                Changes = entry.Entity is BusinessProfile
                    ? null
                    : JsonSerializer.Serialize(
                        entry.CurrentValues.ToObject())
            };

            auditEntries.Add(auditEntry);
        }

        AuditLogs.AddRange(auditEntries);
    }

    private string GetCurrentUserIdentifier()
    {
        var user = _httpContextAccessor?.HttpContext?.User;

        return user?.FindFirstValue(ClaimTypes.Name)
            ?? user?.FindFirstValue(ClaimTypes.Email)
            ?? user?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? "System";
    }

    private static string GetAuditAction(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        if (entry.Entity is Sale && entry.State == EntityState.Added)
        {
            return "Sold";
        }

        if (entry.Entity is StockAdjustment
            && entry.State == EntityState.Added)
        {
            return "StockAdjusted";
        }

        if (entry.Entity is QuotePayment
            && entry.State == EntityState.Added)
        {
            return "PaymentRecorded";
        }

        if (entry.Entity is Quote && entry.State == EntityState.Added)
        {
            return "QuoteCreated";
        }

        if (entry.Entity is Product
            && entry.State == EntityState.Modified)
        {
            var deletedProperty = entry.Property(nameof(Product.IsDeleted));

            if (deletedProperty.IsModified
                && deletedProperty.CurrentValue is true)
            {
                return "Archived";
            }
        }

        return entry.State.ToString();
    }

    private static string? GetEntityId(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var idProperty = entry.Properties.FirstOrDefault(
            property =>
                string.Equals(
                    property.Metadata.Name,
                    "Id",
                    StringComparison.OrdinalIgnoreCase));

        return idProperty?.CurrentValue?.ToString();
    }

    private static string GetAuditSummary(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry,
        string action)
    {
        return entry.Entity switch
        {
            Sale sale =>
                $"Sold {sale.Quantity} unit(s) of product {sale.ProductId}.",

            StockAdjustment adjustment =>
                $"Stock changed by {adjustment.QuantityChange} unit(s) " +
                $"for product {adjustment.ProductId}. Reason: {adjustment.Reason}",

            Product product when action == "Archived" =>
                $"Product '{product.Name}' was archived. " +
                $"Archived by: {product.DeletedBy ?? "Unknown User"}.",

            Product product =>
                $"Product '{product.Name}' {action.ToLowerInvariant()}.",

            Quote quote =>
                $"Quote {quote.QuoteNumber} {action.ToLowerInvariant()}.",

            QuotePayment payment =>
                $"Payment of {payment.Amount:0.00} recorded for quote {payment.QuoteId}.",

            BusinessProfile =>
                $"Business settings {action.ToLowerInvariant()}.",

            Supplier supplier =>
                $"Supplier '{supplier.CompanyName}' {action.ToLowerInvariant()}.",

            StaffMember staff =>
                $"Staff member '{staff.FullName}' {action.ToLowerInvariant()}.",

            _ =>
                $"{entry.Entity.GetType().Name} {action.ToLowerInvariant()}."
        };
    }

    private static bool IsIdentityEntity(Type entityType)
    {
        return entityType.Namespace?.StartsWith(
            "Microsoft.AspNetCore.Identity",
            StringComparison.Ordinal) == true;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(log => log.EntityName).HasMaxLength(100);
            entity.Property(log => log.EntityId).HasMaxLength(100);
            entity.Property(log => log.Action).HasMaxLength(100);
            entity.Property(log => log.UserId).HasMaxLength(256);
            entity.Property(log => log.IpAddress).HasMaxLength(100);
            entity.HasIndex(log => log.Timestamp);
            entity.HasIndex(log => new { log.EntityName, log.Action });
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(product => product.Price)
                .HasPrecision(18, 2);

            entity.Property(product => product.Barcode)
                .HasMaxLength(32);

            entity.Property(product => product.DeletedBy)
                .HasMaxLength(256);

            entity.HasIndex(product => product.Barcode)
                .IsUnique()
                .HasFilter("[Barcode] IS NOT NULL");
        });
        modelBuilder.Entity<Sale>(entity =>
        {
            entity.Property(sale => sale.TotalPrice).HasPrecision(18, 2);
            entity.HasIndex(sale => sale.ReceiptNumber);

            entity.HasOne(sale => sale.Customer)
                .WithMany(customer => customer.Sales)
                .HasForeignKey(sale => sale.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(sale => sale.Salesperson)
                .WithMany(staff => staff.Sales)
                .HasForeignKey(sale => sale.SalespersonId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(sale => sale.SalespersonName)
                .HasMaxLength(200);
        });

        modelBuilder.Entity<BusinessProfile>(entity =>
        {
            entity.Property(profile => profile.DefaultVatRate)
                .HasPrecision(5, 2);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(customer => customer.Email);
            entity.HasIndex(customer => customer.PhoneNumber);
        });

        modelBuilder.Entity<Quote>(entity =>
        {
            entity.Property(quote => quote.Status)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.Property(quote => quote.Subtotal)
                .HasPrecision(18, 2);

            entity.Property(quote => quote.VatRate)
                .HasPrecision(5, 2);

            entity.Property(quote => quote.VatAmount)
                .HasPrecision(18, 2);

            entity.Property(quote => quote.Total)
                .HasPrecision(18, 2);

            entity.Property(quote => quote.DepositPercentage)
                .HasPrecision(5, 2);

            entity.Property(quote => quote.DepositRequired)
                .HasPrecision(18, 2);

            entity.Property(quote => quote.SalespersonName)
                .HasMaxLength(200);

            entity.HasIndex(quote => quote.QuoteNumber)
                .IsUnique();

            entity.HasIndex(quote => quote.ValidUntil);

            entity.HasOne(quote => quote.Customer)
                .WithMany(customer => customer.Quotes)
                .HasForeignKey(quote => quote.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(quote => quote.Salesperson)
                .WithMany(staff => staff.Quotes)
                .HasForeignKey(quote => quote.SalespersonId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<QuoteItem>(entity =>
        {
            entity.Property(item => item.UnitPrice)
                .HasPrecision(18, 2);

            entity.Property(item => item.LineTotal)
                .HasPrecision(18, 2);

            entity.HasOne(item => item.Quote)
                .WithMany(quote => quote.Items)
                .HasForeignKey(item => item.QuoteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(item => item.Product)
                .WithMany(product => product.QuoteItems)
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StaffMember>(entity =>
        {
            entity.Property(staff => staff.FullName)
                .HasMaxLength(200);

            entity.Property(staff => staff.Email)
                .HasMaxLength(256);

            entity.Property(staff => staff.PhoneNumber)
                .HasMaxLength(50);

            entity.Property(staff => staff.Role)
                .HasMaxLength(100);

            entity.HasIndex(staff => staff.Email);
        });

        modelBuilder.Entity<QuotePayment>(entity =>
        {
            entity.Property(payment => payment.Amount)
                .HasPrecision(18, 2);

            entity.Property(payment => payment.Method)
                .HasMaxLength(50);

            entity.Property(payment => payment.Reference)
                .HasMaxLength(100);

            entity.HasOne(payment => payment.Quote)
                .WithMany(quote => quote.Payments)
                .HasForeignKey(payment => payment.QuoteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeliveryLog>(entity =>
        {
            entity.Property(log => log.Channel)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.Property(log => log.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.HasIndex(log => new { log.DocumentType, log.DocumentId });
        });

        modelBuilder.Entity<StockAdjustment>(entity =>
        {
            entity.Property(adjustment => adjustment.Reason)
                .HasMaxLength(500);

            entity.Property(adjustment => adjustment.PerformedBy)
                .HasMaxLength(256);

            entity.HasIndex(adjustment => adjustment.CreatedAt);

            entity.HasOne(adjustment => adjustment.Product)
                .WithMany(product => product.StockAdjustments)
                .HasForeignKey(adjustment => adjustment.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        base.OnModelCreating(modelBuilder);
    }
}