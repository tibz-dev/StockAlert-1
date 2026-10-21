using Microsoft.EntityFrameworkCore;
using StockAlert.Application.DTOs;
using StockAlert.Application.Interfaces;
using StockAlert.Domain.Entities;

namespace StockAlert.Infrastructure.Services;

public class BusinessProfileService : IBusinessProfileService
{
    private readonly IApplicationDbContext _context;

    public BusinessProfileService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BusinessProfileDto> GetAsync()
    {
        var profile = await GetOrCreateEntityAsync();
        return ToDto(profile);
    }

    public async Task<BusinessProfileDto> UpdateAsync(
        UpdateBusinessProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BusinessName))
        {
            throw new ArgumentException("Business name is required.");
        }

        if (request.DefaultVatRate < 0 || request.DefaultVatRate > 100)
        {
            throw new ArgumentException("VAT rate must be between 0 and 100.");
        }

        if (request.QuoteValidityDays < 1 || request.QuoteValidityDays > 365)
        {
            throw new ArgumentException(
                "Quote validity must be between 1 and 365 days.");
        }

        var profile = await GetOrCreateEntityAsync();

        profile.BusinessName = request.BusinessName.Trim();
        profile.TradingName = Normalize(request.TradingName);
        profile.RegistrationNumber = Normalize(request.RegistrationNumber);
        profile.VatNumber = Normalize(request.VatNumber);
        profile.IsVatRegistered = request.IsVatRegistered;
        profile.DefaultVatRate = request.IsVatRegistered
            ? request.DefaultVatRate
            : 0m;
        profile.Email = Normalize(request.Email);
        profile.PhoneNumber = Normalize(request.PhoneNumber);
        profile.WhatsAppNumber = Normalize(request.WhatsAppNumber);
        profile.Website = Normalize(request.Website);
        profile.LogoUrl = Normalize(request.LogoUrl);
        profile.AddressLine1 = Normalize(request.AddressLine1);
        profile.AddressLine2 = Normalize(request.AddressLine2);
        profile.City = Normalize(request.City);
        profile.Province = Normalize(request.Province);
        profile.PostalCode = Normalize(request.PostalCode);
        profile.Country = string.IsNullOrWhiteSpace(request.Country)
            ? "South Africa"
            : request.Country.Trim();
        profile.BranchName = Normalize(request.BranchName);
        profile.BranchNumber = Normalize(request.BranchNumber);
        profile.BankName = Normalize(request.BankName);
        profile.BankAccountName = Normalize(request.BankAccountName);
        profile.BankAccountNumber = Normalize(request.BankAccountNumber);
        profile.BankBranchCode = Normalize(request.BankBranchCode);
        profile.BankAccountType = Normalize(request.BankAccountType);
        profile.CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode)
            ? "ZAR"
            : request.CurrencyCode.Trim().ToUpperInvariant();
        profile.QuoteValidityDays = request.QuoteValidityDays;
        profile.ReceiptFooter = Normalize(request.ReceiptFooter);
        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(default);
        return ToDto(profile);
    }

    private async Task<BusinessProfile> GetOrCreateEntityAsync()
    {
        var profile = await _context.BusinessProfiles
            .FirstOrDefaultAsync();

        if (profile != null)
        {
            return profile;
        }

        profile = new BusinessProfile
        {
            Id = Guid.NewGuid(),
            BusinessName = "StockAlert Business",
            Country = "South Africa",
            CurrencyCode = "ZAR",
            QuoteValidityDays = 14,
            UpdatedAt = DateTime.UtcNow
        };

        _context.BusinessProfiles.Add(profile);
        await _context.SaveChangesAsync(default);

        return profile;
    }

    private static BusinessProfileDto ToDto(BusinessProfile profile)
    {
        return new BusinessProfileDto(
            profile.Id,
            profile.BusinessName,
            profile.TradingName,
            profile.RegistrationNumber,
            profile.VatNumber,
            profile.IsVatRegistered,
            profile.DefaultVatRate,
            profile.Email,
            profile.PhoneNumber,
            profile.WhatsAppNumber,
            profile.Website,
            profile.LogoUrl,
            profile.AddressLine1,
            profile.AddressLine2,
            profile.City,
            profile.Province,
            profile.PostalCode,
            profile.Country,
            profile.BranchName,
            profile.BranchNumber,
            profile.BankName,
            profile.BankAccountName,
            profile.BankAccountNumber,
            profile.BankBranchCode,
            profile.BankAccountType,
            profile.CurrencyCode,
            profile.QuoteValidityDays,
            profile.ReceiptFooter,
            profile.UpdatedAt
        );
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
