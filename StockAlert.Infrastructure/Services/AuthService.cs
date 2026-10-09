using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StockAlert.Application.DTOs;
using StockAlert.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace StockAlert.Infrastructure.Services;

public class AuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration config)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _config = config;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return new AuthResponse(false, "", "Invalid Credentials");
        }

        await EnsureRolesAsync();
        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Count == 0)
        {
            var owners = await _userManager.GetUsersInRoleAsync("Owner");

            if (owners.Count == 0)
            {
                var bootstrapOwnerEmail =
                    _config["Security:BootstrapOwnerEmail"]?.Trim();

                var totalUsers = await _userManager.Users.CountAsync();

                var canBootstrapOwner =
                    (!string.IsNullOrWhiteSpace(bootstrapOwnerEmail)
                        && string.Equals(
                            user.Email,
                            bootstrapOwnerEmail,
                            StringComparison.OrdinalIgnoreCase))
                    || totalUsers == 1;

                if (!canBootstrapOwner)
                {
                    return new AuthResponse(
                        false,
                        "",
                        "No owner is configured. Set Security:BootstrapOwnerEmail to the authorised owner email.");
                }

                await _userManager.AddToRoleAsync(user, "Owner");
                roles = new[] { "Owner" };
            }
            else
            {
                await _userManager.AddToRoleAsync(user, "Staff");
                roles = new[] { "Staff" };
            }
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.FullName)
        };

        claims.AddRange(
            roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]!);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = _config["Jwt:Issuer"],
            Audience = _config["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return new AuthResponse(true, tokenHandler.WriteToken(token), "Success");
    }

    public async Task<AuthResponse> RegisterAsync(
        string email,
        string password,
        string fullName)
    {
        await EnsureRolesAsync();

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            return new AuthResponse(
                false,
                "",
                "Email already registered");
        }

        var hasAnyUsers = await _userManager.Users.AnyAsync();

        if (hasAnyUsers)
        {
            return new AuthResponse(
                false,
                "",
                "Public registration is closed. The business owner must create staff access.");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName
        };

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            return new AuthResponse(
                false,
                "",
                string.Join(
                    ", ",
                    result.Errors.Select(error => error.Description)));
        }

        await _userManager.AddToRoleAsync(user, "Owner");

        return new AuthResponse(
            true,
            "",
            "Owner account created successfully. Please login.");
    }

    private async Task EnsureRolesAsync()
    {
        foreach (var role in new[] { "Owner", "Manager", "Sales", "Stock", "Staff" })
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}