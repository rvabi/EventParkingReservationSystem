using EventParking.Business.DTOs.Auth;
using EventParking.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventParking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        IAuthService authService,
        IWebHostEnvironment environment)
    {
        _authService = authService;
        _environment = environment;
    }

    // POST: api/auth/register
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request)
    {
        var result =
            await _authService.RegisterAsync(request);

        if (!result.Success)
        {
            return Conflict(new
            {
                message = result.Message
            });
        }

        return Ok(new
        {
            message = result.Message,

            // Only exposed locally for Swagger testing.
            verificationToken =
                _environment.IsDevelopment()
                    ? result.Token
                    : null
        });
    }

    // GET: api/auth/verify-email?token=...
    [HttpGet("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail(
        [FromQuery] string token)
    {
        var result =
            await _authService.VerifyEmailAsync(token);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Message
            });
        }

        return Ok(new
        {
            message = result.Message
        });
    }

    // POST: api/auth/resend-verification
    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendVerification(
        [FromBody] ResendVerificationRequest request)
    {
        var result =
            await _authService.ResendVerificationAsync(
                request);

        return Ok(new
        {
            message = result.Message,

            verificationToken =
                _environment.IsDevelopment()
                    ? result.Token
                    : null
        });
    }

    // POST: api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        var result =
            await _authService.LoginAsync(request);

        if (!result.Success)
        {
            return Unauthorized(new
            {
                message = result.Message
            });
        }

        return Ok(new
        {
            message = result.Message,
            data = result.Data
        });
    }

    // POST: api/auth/forgot-password
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request)
    {
        var result =
            await _authService.ForgotPasswordAsync(
                request);

        return Ok(new
        {
            message = result.Message,

            // Temporary local testing only.
            resetToken =
                _environment.IsDevelopment()
                    ? result.Token
                    : null
        });
    }

    // POST: api/auth/reset-password
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request)
    {
        var result =
            await _authService.ResetPasswordAsync(
                request);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Message
            });
        }

        return Ok(new
        {
            message = result.Message
        });
    }
}