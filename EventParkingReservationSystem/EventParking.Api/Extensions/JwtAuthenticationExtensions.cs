using EventParking.Api.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace EventParking.Api.Extensions;

public static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddSharedJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtOptions =
            configuration
                .GetSection(JwtOptions.SectionName)
                .Get<JwtOptions>()
            ?? throw new InvalidOperationException(
                "JWT configuration is missing.");

        if (string.IsNullOrWhiteSpace(jwtOptions.Key) ||
            string.IsNullOrWhiteSpace(jwtOptions.Issuer) ||
            string.IsNullOrWhiteSpace(jwtOptions.Audience) 
            )
        {
            throw new InvalidOperationException(
                "JWT configuration is incomplete.");
        }

        services.Configure<JwtOptions>(
            configuration.GetSection(
                JwtOptions.SectionName));

        services
            .AddAuthentication(
                JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtOptions.Issuer,

                        ValidateAudience = true,
                        ValidAudience = jwtOptions.Audience,

                        ValidateLifetime = true,

                        ValidateIssuerSigningKey = true,

                        IssuerSigningKey =
                            new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(
                                    jwtOptions.Key)),

                        ClockSkew = TimeSpan.Zero,

                        NameClaimType =
                            ClaimTypes.NameIdentifier,

                        RoleClaimType =
                            ClaimTypes.Role
                    };
            });

        services.AddAuthorization();

        return services;
    }
}
