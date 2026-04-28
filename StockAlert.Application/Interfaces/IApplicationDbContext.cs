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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}