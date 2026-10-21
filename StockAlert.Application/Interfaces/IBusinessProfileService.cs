using StockAlert.Application.DTOs;

namespace StockAlert.Application.Interfaces;

public interface IBusinessProfileService
{
    Task<BusinessProfileDto> GetAsync();
    Task<BusinessProfileDto> UpdateAsync(UpdateBusinessProfileRequest request);
}
