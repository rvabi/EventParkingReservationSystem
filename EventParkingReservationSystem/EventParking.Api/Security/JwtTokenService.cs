using EventParking.Business.DTOs.Auth;
using EventParking.Business.Interfaces;
using EventParking.Models.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EventParking.Api.Security;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public JwtTokenResult GenerateToken(Customer customer)
    {
        string key =
            _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "JWT signing key is not configured.");

        string issuer =
            _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "JWT issuer is not configured.");

        string audience =
            _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "JWT audience is not configured.");

        int expiryMinutes =
            _configuration.GetValue<int?>(
                "Jwt:ExpiryMinutes") ?? 60;

        DateTime expiresAt =
            DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new List<Claim>
        {
            new(
                JwtRegisteredClaimNames.Sub,
                customer.Id.ToString()),

            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString()),

            new(
                ClaimTypes.NameIdentifier,
                customer.Id.ToString()),

            new(
                ClaimTypes.Name,
                customer.FullName),

            new(
                ClaimTypes.Email,
                customer.Email),

            new(
                ClaimTypes.Role,
                customer.Role.ToString())
        };

        var securityKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(key));

        var credentials =
            new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

        var token =
            new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiresAt,
                signingCredentials: credentials);

        string tokenValue =
            new JwtSecurityTokenHandler()
                .WriteToken(token);

        return new JwtTokenResult
        {
            Token = tokenValue,
            ExpiresAt = expiresAt
        };
    }
}
