using EventParking.Api.Security;
using Microsoft.OpenApi.Models;
using EventParking.Api.Middleware;
using EventParking.Api.Extensions;

using EventParking.Business.Interfaces;
using EventParking.Business.Services;

using EventParking.DataAccess.Context;
using EventParking.DataAccess.Interfaces;
using EventParking.DataAccess.Repositories;
using EventParking.DataAccess.Seed;

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "DefaultConnection is not configured.");

builder.Services.AddControllers();

builder.Services.AddSharedApiFoundation();

builder.Services.AddSharedJwtAuthentication(
    builder.Configuration);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description =
                "Enter the JWT token received from the login endpoint."
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference =
                        new OpenApiReference
                        {
                            Type =
                                ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                },
                Array.Empty<string>()
            }
        });
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// Customer
builder.Services.AddScoped<
    ICustomerRepository,
    CustomerRepository>();

builder.Services.AddScoped<
    ICustomerService,
    CustomerService>();

// Event / Seat
builder.Services.AddScoped<
    IEventRepository,
    EventRepository>();

builder.Services.AddScoped<
    ISeatRepository,
    SeatRepository>();

builder.Services.AddScoped<
    ISeatService,
    SeatService>();

// Authentication
builder.Services.AddScoped<
    IPasswordService,
    PasswordService>();

builder.Services.AddScoped<
    ISecurityTokenService,
    SecurityTokenService>();

builder.Services.AddScoped<
    IJwtTokenService,
    JwtTokenService>();

builder.Services.AddScoped<
    IAuthService,
    AuthService>();

// Venue / Category / Facility
builder.Services.AddScoped<
    IVenueRepository,
    VenueRepository>();

builder.Services.AddScoped<
    IEventCategoryRepository,
    EventCategoryRepository>();

builder.Services.AddScoped<
    IVenueFacilityRepository,
    VenueFacilityRepository>();

builder.Services.AddScoped<
    IVenueService,
    VenueService>();

builder.Services.AddScoped<
    IEventCategoryService,
    EventCategoryService>();

builder.Services.AddScoped<
    IEventService,
    EventService>();

builder.Services.AddScoped<
    IVenueFacilityService,
    VenueFacilityService>();

// Parking
builder.Services.AddScoped<
    IParkingSlotRepository,
    ParkingSlotRepository>();

builder.Services.AddScoped<
    IParkingSlotService,
    ParkingSlotService>();

builder.Services.AddScoped<
    IParkingReservationRepository,
    ParkingReservationRepository>();

builder.Services.AddScoped<
    IParkingReservationService,
    ParkingReservationService>();

// Food Court
builder.Services.AddScoped<
    IFoodStallRepository,
    FoodStallRepository>();

builder.Services.AddScoped<
    IFoodItemRepository,
    FoodItemRepository>();

builder.Services.AddScoped<
    IFoodOrderRepository,
    FoodOrderRepository>();

builder.Services.AddScoped<
    IFoodStallService,
    FoodStallService>();

builder.Services.AddScoped<
    IFoodItemService,
    FoodItemService>();

builder.Services.AddScoped<
    IFoodOrderService,
    FoodOrderService>();

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var dbContext =
        scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

    await DatabaseSeeder.SeedAsync(dbContext);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(
    SharedApiServiceExtensions.FrontendCorsPolicy);

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();