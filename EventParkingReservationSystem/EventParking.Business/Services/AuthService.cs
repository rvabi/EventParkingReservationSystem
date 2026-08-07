using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventParking.Business.DTOs.Auth;
using EventParking.Business.Interfaces;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using EventParking.Models.Enums;

namespace EventParking.Business.Services;

public class AuthService : IAuthService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IPasswordService _passwordService;
    private readonly ISecurityTokenService _securityTokenService;

    public AuthService(
        ICustomerRepository customerRepository,
        IPasswordService passwordService,
        ISecurityTokenService securityTokenService)
    {
        _customerRepository = customerRepository;
        _passwordService = passwordService;
        _securityTokenService = securityTokenService;
    }

    public async Task<AuthResult> RegisterAsync(
        RegisterRequest request)
    {
        string normalizedEmail =
            request.Email.Trim().ToLowerInvariant();

        bool emailExists =
            await _customerRepository.EmailExistsAsync(
                normalizedEmail);

        if (emailExists)
        {
            return new AuthResult
            {
                Success = false,
                Message = "An account with this email already exists."
            };
        }

        string rawVerificationToken =
            _securityTokenService.GenerateToken();

        string verificationTokenHash =
            _securityTokenService.HashToken(
                rawVerificationToken);

        var customer = new Customer
        {
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            Phone = request.Phone.Trim(),

            PasswordHash =
                _passwordService.HashPassword(
                    request.Password),

            Role = UserRole.Customer,
            Status = CustomerStatus.Active,

            EmailVerified = false,

            EmailVerificationTokenHash =
                verificationTokenHash,

            EmailVerificationTokenExpiresAt =
                DateTime.UtcNow.AddHours(24),

            CreatedAt = DateTime.UtcNow
        };

        await _customerRepository.AddAsync(customer);
        await _customerRepository.SaveChangesAsync();

        return new AuthResult
        {
            Success = true,
            Message =
                "Registration successful. Please verify your email.",

            Token = rawVerificationToken
        };
    }

    public async Task<AuthResult> VerifyEmailAsync(
        string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new AuthResult
            {
                Success = false,
                Message = "Verification token is required."
            };
        }

        string tokenHash =
            _securityTokenService.HashToken(token);

        var customer =
            await _customerRepository
                .GetByEmailVerificationTokenHashAsync(
                    tokenHash);

        if (customer is null)
        {
            return new AuthResult
            {
                Success = false,
                Message =
                    "Invalid or already used verification token."
            };
        }

        if (customer.EmailVerified)
        {
            customer.EmailVerificationTokenHash = null;
            customer.EmailVerificationTokenExpiresAt = null;
            customer.UpdatedAt = DateTime.UtcNow;

            _customerRepository.Update(customer);
            await _customerRepository.SaveChangesAsync();

            return new AuthResult
            {
                Success = false,
                Message = "Email is already verified."
            };
        }

        if (!customer.EmailVerificationTokenExpiresAt.HasValue ||
            customer.EmailVerificationTokenExpiresAt.Value
                < DateTime.UtcNow)
        {
            return new AuthResult
            {
                Success = false,
                Message =
                    "Verification token has expired. Request a new verification token."
            };
        }

        customer.EmailVerified = true;

        // Makes verification token single-use.
        customer.EmailVerificationTokenHash = null;
        customer.EmailVerificationTokenExpiresAt = null;

        customer.UpdatedAt = DateTime.UtcNow;

        _customerRepository.Update(customer);
        await _customerRepository.SaveChangesAsync();

        return new AuthResult
        {
            Success = true,
            Message = "Email verified successfully."
        };
    }

    public async Task<AuthResult> ResendVerificationAsync(
        ResendVerificationRequest request)
    {
        string normalizedEmail =
            request.Email.Trim().ToLowerInvariant();

        var customer =
            await _customerRepository.GetByEmailAsync(
                normalizedEmail);

        /*
         * Generic response prevents outsiders from discovering
         * whether an account exists.
         */
        if (customer is null)
        {
            return new AuthResult
            {
                Success = true,
                Message =
                    "If the account exists and requires verification, a new verification token has been generated."
            };
        }

        if (customer.EmailVerified)
        {
            return new AuthResult
            {
                Success = true,
                Message =
                    "If the account exists and requires verification, a new verification token has been generated."
            };
        }

        string rawVerificationToken =
            _securityTokenService.GenerateToken();

        string verificationTokenHash =
            _securityTokenService.HashToken(
                rawVerificationToken);

        // Replacing the old hash invalidates the old link.
        customer.EmailVerificationTokenHash =
            verificationTokenHash;

        customer.EmailVerificationTokenExpiresAt =
            DateTime.UtcNow.AddHours(24);

        customer.UpdatedAt = DateTime.UtcNow;

        _customerRepository.Update(customer);
        await _customerRepository.SaveChangesAsync();

        return new AuthResult
        {
            Success = true,

            Message =
                "If the account exists and requires verification, a new verification token has been generated.",

            Token = rawVerificationToken
        };
    }
}
