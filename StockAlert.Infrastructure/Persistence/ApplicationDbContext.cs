using Microsoft.EntityFrameworkCore;
using StockAlert.Application.Interfaces;
using StockAlert.Domain.Entities;

namespace StockAlert.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Sale> Sales => Set<Sale>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
       
        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);

       
        modelBuilder.Entity<Sale>()
            .Property(s => s.TotalPrice)
            .HasPrecision(18, 2);

        base.OnModelCreating(modelBuilder);
    }
}