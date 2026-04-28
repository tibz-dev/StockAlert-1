using Microsoft.AspNetCore.Identity; 

namespace StockAlert.Domain.Entities;


public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}