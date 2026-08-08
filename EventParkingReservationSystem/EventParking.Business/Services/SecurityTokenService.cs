using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventParking.Business.Interfaces;
using System.Security.Cryptography;

namespace EventParking.Business.Services;

public class SecurityTokenService : ISecurityTokenService
{
    public string GenerateToken()
    {
        byte[] tokenBytes = RandomNumberGenerator.GetBytes(32);

        return Convert.ToHexString(tokenBytes);
    }

    public string HashToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException(
                "Token cannot be empty.",
                nameof(token));
        }

        byte[] tokenBytes = Encoding.UTF8.GetBytes(token);

        byte[] hashBytes = SHA256.HashData(tokenBytes);

        return Convert.ToHexString(hashBytes);
    }
}
