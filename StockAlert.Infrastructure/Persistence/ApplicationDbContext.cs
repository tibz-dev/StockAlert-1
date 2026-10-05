using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StockAlert.Application.Interfaces;
using StockAlert.Domain.Entities;
using System.Text.Json;

namespace StockAlert.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    
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
            if (entry.Entity is AuditLog || entry.Entity is StockAdjustment || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            var auditEntry = new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityName = entry.Entity.GetType().Name,
                Action = entry.State.ToString(),
                UserId = "System", // We will update this once we add JWT
                Timestamp = DateTime.UtcNow,
                Changes = JsonSerializer.Serialize(entry.CurrentValues.ToObject())
            };

            auditEntries.Add(auditEntry);
        }

        AuditLogs.AddRange(auditEntries);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
        modelBuilder.Entity<Sale>().Property(s => s.TotalPrice).HasPrecision(18, 2);

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