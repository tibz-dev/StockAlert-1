using Microsoft.EntityFrameworkCore;
using StockAlert.Application.DTOs;
using StockAlert.Application.Interfaces;
using StockAlert.Domain.Entities;

namespace StockAlert.Infrastructure.Services;

public class StaffService : IStaffService
{
    private readonly IApplicationDbContext _context;

    public StaffService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<StaffMemberDto>> GetAllAsync(bool activeOnly = false)
    {
        var query = _context.StaffMembers.AsNoTracking();

        if (activeOnly)
        {
            query = query.Where(staff => staff.IsActive);
        }

        return await query
            .OrderBy(staff => staff.FullName)
            .Select(staff => new StaffMemberDto(
                staff.Id,
                staff.FullName,
                staff.Email,
                staff.PhoneNumber,
                staff.Role,
                staff.IsActive,
                staff.CreatedAt))
            .ToListAsync();
    }

    public async Task<StaffMemberDto?> GetByIdAsync(Guid id)
    {
        return await _context.StaffMembers
            .AsNoTracking()
            .Where(staff => staff.Id == id)
            .Select(staff => new StaffMemberDto(
                staff.Id,
                staff.FullName,
                staff.Email,
                staff.PhoneNumber,
                staff.Role,
                staff.IsActive,
                staff.CreatedAt))
            .FirstOrDefaultAsync();
    }

    public async Task<Guid> CreateAsync(CreateStaffMemberRequest request)
    {
        Validate(request.FullName, request.Role);

        var email = Normalize(request.Email);

        if (email != null &&
            await _context.StaffMembers.AnyAsync(staff => staff.Email == email))
        {
            throw new InvalidOperationException(
                "A staff member with this email already exists.");
        }

        var staff = new StaffMember
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = email,
            PhoneNumber = Normalize(request.PhoneNumber),
            Role = request.Role.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.StaffMembers.Add(staff);
        await _context.SaveChangesAsync(default);

        return staff.Id;
    }

    public async Task<StaffMemberDto?> UpdateAsync(
        Guid id,
        UpdateStaffMemberRequest request)
    {
        Validate(request.FullName, request.Role);

        var staff = await _context.StaffMembers
            .FirstOrDefaultAsync(item => item.Id == id);

        if (staff == null)
        {
            return null;
        }

        var email = Normalize(request.Email);

        if (email != null &&
            await _context.StaffMembers.AnyAsync(item =>
                item.Id != id && item.Email == email))
        {
            throw new InvalidOperationException(
                "A staff member with this email already exists.");
        }

        staff.FullName = request.FullName.Trim();
        staff.Email = email;
        staff.PhoneNumber = Normalize(request.PhoneNumber);
        staff.Role = request.Role.Trim();
        staff.IsActive = request.IsActive;

        await _context.SaveChangesAsync(default);

        return ToDto(staff);
    }

    private static StaffMemberDto ToDto(StaffMember staff)
    {
        return new StaffMemberDto(
            staff.Id,
            staff.FullName,
            staff.Email,
            staff.PhoneNumber,
            staff.Role,
            staff.IsActive,
            staff.CreatedAt
        );
    }

    private static void Validate(string fullName, string role)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Staff member name is required.");
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("Staff role is required.");
        }
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
