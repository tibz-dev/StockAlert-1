using Microsoft.EntityFrameworkCore;
using StockAlert.Domain.Entities;
using System.Collections.Generic;

namespace StockAlert.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }
    DbSet<Category> Categories { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<Sale> Sales { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<StockAdjustment> StockAdjustments { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}