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
    DbSet<CheckoutTransaction> CheckoutTransactions { get; }
    DbSet<CheckoutLine> CheckoutLines { get; }
    DbSet<CheckoutTender> CheckoutTenders { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<StockAdjustment> StockAdjustments { get; }
    DbSet<BusinessProfile> BusinessProfiles { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Quote> Quotes { get; }
    DbSet<QuoteItem> QuoteItems { get; }
    DbSet<DeliveryLog> DeliveryLogs { get; }
    DbSet<QuotePayment> QuotePayments { get; }
    DbSet<StaffMember> StaffMembers { get; }
    DbSet<OwnerReportSettings> OwnerReportSettings { get; }
    DbSet<StocktakeSession> StocktakeSessions { get; }
    DbSet<StocktakeItem> StocktakeItems { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}