using StockAlert.Application.DTOs;

namespace StockAlert.Application.Interfaces;

public interface IStaffService
{
    Task<IReadOnlyList<StaffMemberDto>> GetAllAsync(bool activeOnly = false);
    Task<StaffMemberDto?> GetByIdAsync(Guid id);
    Task<Guid> CreateAsync(CreateStaffMemberRequest request);
    Task<StaffMemberDto?> UpdateAsync(Guid id, UpdateStaffMemberRequest request);
}
