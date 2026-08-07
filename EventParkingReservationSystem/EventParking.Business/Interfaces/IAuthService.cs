using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventParking.Business.DTOs.Auth;


namespace EventParking.Business.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request);

    Task<AuthResult> VerifyEmailAsync(string token);

    Task<AuthResult> ResendVerificationAsync(
        ResendVerificationRequest request);

    Task<AuthResult> ForgotPasswordAsync(
        ForgotPasswordRequest request);

    Task<AuthResult> ResetPasswordAsync(
        ResetPasswordRequest request);
}
