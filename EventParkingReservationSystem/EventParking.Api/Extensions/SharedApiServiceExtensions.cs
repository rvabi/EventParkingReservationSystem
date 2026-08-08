using EventParking.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace EventParking.Api.Extensions;

public static class SharedApiServiceExtensions
{
    public const string FrontendCorsPolicy =
        "FrontendCorsPolicy";

    public static IServiceCollection AddSharedApiFoundation(
        this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(
                FrontendCorsPolicy,
                policy =>
                {
                    policy
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
        });

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(item =>
                        item.Value?.Errors.Count > 0)
                    .ToDictionary(
                        item => item.Key,
                        item => item.Value!.Errors
                            .Select(error =>
                                string.IsNullOrWhiteSpace(
                                    error.ErrorMessage)
                                    ? "Invalid value."
                                    : error.ErrorMessage)
                            .ToArray());

                var response =
                    ApiResponse<object>.Fail(
                        "Validation failed.",
                        errors);

                return new BadRequestObjectResult(response);
            };
        });

        return services;
    }
}
