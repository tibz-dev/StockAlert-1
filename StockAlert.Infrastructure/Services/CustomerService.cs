using Microsoft.EntityFrameworkCore;
using StockAlert.Application.DTOs;
using StockAlert.Application.Interfaces;

namespace StockAlert.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly IApplicationDbContext _context;

    public CustomerService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CustomerSummaryDto>> GetAllAsync()
    {
        var customers = await _context.Customers
            .AsNoTracking()
            .OrderBy(customer => customer.FullName)
            .ToListAsync();

        return await BuildSummariesAsync(customers.Select(customer => customer.Id).ToList());
    }

    public async Task<CustomerSummaryDto?> GetByIdAsync(Guid id)
    {
        var summaries = await BuildSummariesAsync(new List<Guid> { id });
        return summaries.FirstOrDefault();
    }

    private async Task<List<CustomerSummaryDto>> BuildSummariesAsync(List<Guid> customerIds)
    {
        if (customerIds.Count == 0)
        {
            return new List<CustomerSummaryDto>();
        }

        var customers = await _context.Customers
            .AsNoTracking()
            .Where(customer => customerIds.Contains(customer.Id))
            .ToListAsync();

        var quoteStats = await _context.Quotes
            .AsNoTracking()
            .Where(quote => customerIds.Contains(quote.CustomerId))
            .GroupBy(quote => quote.CustomerId)
            .Select(group => new
            {
                CustomerId = group.Key,
                Count = group.Count(),
                Last = group.Max(quote => quote.CreatedAt)
            })
            .ToDictionaryAsync(item => item.CustomerId);

        var saleStats = await _context.Sales
            .AsNoTracking()
            .Where(sale => sale.CustomerId.HasValue && customerIds.Contains(sale.CustomerId.Value))
            .GroupBy(sale => sale.CustomerId!.Value)
            .Select(group => new
            {
                CustomerId = group.Key,
                Count = group.Count(),
                LifetimeValue = group.Sum(sale => sale.TotalPrice),
                Last = group.Max(sale => sale.SaleDate)
            })
            .ToDictionaryAsync(item => item.CustomerId);

        return customers
            .Select(customer =>
            {
                quoteStats.TryGetValue(customer.Id, out var quote);
                saleStats.TryGetValue(customer.Id, out var sale);

                DateTime? lastActivity = null;
                if (quote != null) lastActivity = quote.Last;
                if (sale != null && (!lastActivity.HasValue || sale.Last > lastActivity))
                {
                    lastActivity = sale.Last;
                }

                return new CustomerSummaryDto(
                    customer.Id,
                    customer.FullName,
                    customer.CompanyName,
                    customer.Email,
                    customer.PhoneNumber,
                    customer.WhatsAppNumber,
                    customer.HasWhatsApp,
                    customer.Address,
                    quote?.Count ?? 0,
                    sale?.Count ?? 0,
                    sale?.LifetimeValue ?? 0m,
                    lastActivity
                );
            })
            .OrderByDescending(customer => customer.LastActivityAt)
            .ThenBy(customer => customer.FullName)
            .ToList();
    }
}
