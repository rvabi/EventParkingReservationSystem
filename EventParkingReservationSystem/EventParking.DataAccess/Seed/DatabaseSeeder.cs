using EventParking.DataAccess.Context;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventParking.DataAccess.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context)
    {
        await SeedUsersAsync(context);
        await SeedEventDataAsync(context);
    }


    // =========================================================
    // USERS
    // =========================================================

    private static async Task SeedUsersAsync(
        ApplicationDbContext context)
    {
        const string adminEmail =
            "admin@eventparking.local";

        if (!await context.Customers.AnyAsync(
                customer =>
                    customer.Email == adminEmail))
        {
            var admin = new Customer
            {
                FullName = "System Administrator",

                Email = adminEmail,

                Phone = "0700000000",

                Role = UserRole.Administrator,

                Status = CustomerStatus.Active,

                EmailVerified = true,

                CreatedAt = DateTime.UtcNow
            };

            // DEVELOPMENT-ONLY demo credential.
            admin.PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(
                    "Admin@12345");

            await context.Customers.AddAsync(admin);
        }


        const string customerEmail =
            "customer@eventparking.local";

        if (!await context.Customers.AnyAsync(
                customer =>
                    customer.Email == customerEmail))
        {
            var customer = new Customer
            {
                FullName = "Demo Customer",

                Email = customerEmail,

                Phone = "0710000000",

                Role = UserRole.Customer,

                Status = CustomerStatus.Active,

                EmailVerified = true,

                CreatedAt = DateTime.UtcNow
            };

            // DEVELOPMENT-ONLY demo credential.
            customer.PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(
                    "Customer@12345");

            await context.Customers.AddAsync(customer);
        }

        await context.SaveChangesAsync();
    }


    // =========================================================
    // VENUE + CATEGORY + EVENT
    // =========================================================

    private static async Task SeedEventDataAsync(
        ApplicationDbContext context)
    {
        var venue =
            await context.Venues
                .FirstOrDefaultAsync(
                    venue =>
                        venue.Name ==
                        "Vavuniya Event Centre");

        if (venue is null)
        {
            venue = new Venue
            {
                Name =
                    "Vavuniya Event Centre",

                Address =
                    "Vavuniya, Sri Lanka",

                TotalCapacity = 500,

                CreatedAt = DateTime.UtcNow
            };

            await context.Venues.AddAsync(venue);

            await context.SaveChangesAsync();
        }


        var category =
            await context.EventCategories
                .FirstOrDefaultAsync(
                    category =>
                        category.Name ==
                        "Technology");

        if (category is null)
        {
            category = new EventCategory
            {
                Name = "Technology",

                Description =
                    "Technology conferences and exhibitions.",

                CreatedAt = DateTime.UtcNow
            };

            await context.EventCategories
                .AddAsync(category);

            await context.SaveChangesAsync();
        }


        var eventItem =
            await context.Events
                .FirstOrDefaultAsync(
                    eventItem =>
                        eventItem.Name ==
                        "Smart Technology Expo");

        if (eventItem is null)
        {
            var eventStart =
                DateTime.UtcNow.Date
                    .AddDays(30)
                    .AddHours(9);

            eventItem = new Event
            {
                Name =
                    "Smart Technology Expo",

                Description =
                    "Demo technology event for the reservation system.",

                VenueId = venue.Id,

                EventCategoryId =
                    category.Id,

                StartDateTime =
                    eventStart,

                EndDateTime =
                    eventStart.AddHours(6),

                TicketPrice =
                    2500.00m,

                ParkingFee =
                    500.00m,

                Capacity =
                    100,

                CreatedAt =
                    DateTime.UtcNow
            };

            await context.Events
                .AddAsync(eventItem);

            await context.SaveChangesAsync();
        }


        // Demo resources
        await SeedSeatsAsync(
            context,
            eventItem.Id);

        await SeedParkingSlotsAsync(
            context,
            eventItem.Id);

        await SeedFoodCourtAsync(
            context,
            eventItem.Id);

        await SeedVenueFacilitiesAsync(
            context,
            venue.Id);
    }


    // =========================================================
    // SEATS
    // =========================================================

    private static async Task SeedSeatsAsync(
        ApplicationDbContext context,
        int eventId)
    {
        if (await context.Seats.AnyAsync(
                seat =>
                    seat.EventId == eventId))
        {
            return;
        }

        var seats =
            new List<Seat>();

        for (var number = 1;
             number <= 10;
             number++)
        {
            seats.Add(
                new Seat
                {
                    EventId =
                        eventId,

                    SeatNumber =
                        $"A{number}",

                    RowLabel =
                        "A",

                    ColumnNumber =
                        number,

                    SeatType =
                        "Standard",

                    Status =
                        SeatStatus.Available,

                    CreatedAt =
                        DateTime.UtcNow
                });
        }

        await context.Seats
            .AddRangeAsync(seats);

        await context.SaveChangesAsync();
    }


    // =========================================================
    // PARKING
    // =========================================================

    private static async Task SeedParkingSlotsAsync(
        ApplicationDbContext context,
        int eventId)
    {
        if (await context.ParkingSlots.AnyAsync(
                slot =>
                    slot.EventId == eventId))
        {
            return;
        }

        var parkingSlots =
            new List<ParkingSlot>();

        for (var number = 1;
             number <= 5;
             number++)
        {
            parkingSlots.Add(
                new ParkingSlot
                {
                    EventId =
                        eventId,

                    SlotNumber =
                        $"P{number:00}",

                    Zone =
                        "Zone A",

                    Status =
                        ParkingSlotStatus.Available,

                    CreatedAt =
                        DateTime.UtcNow
                });
        }

        await context.ParkingSlots
            .AddRangeAsync(parkingSlots);

        await context.SaveChangesAsync();
    }


    // =========================================================
    // FOOD COURT
    // =========================================================

    private static async Task SeedFoodCourtAsync(
        ApplicationDbContext context,
        int eventId)
    {
        var foodStall =
            await context.FoodStalls
                .FirstOrDefaultAsync(
                    stall =>
                        stall.EventId == eventId &&
                        stall.Name == "Tech Bites");

        if (foodStall is null)
        {
            foodStall =
                new FoodStall
                {
                    EventId =
                        eventId,

                    Name =
                        "Tech Bites",

                    Description =
                        "Snacks and refreshments for event attendees.",

                    Status =
                        "Open",

                    CreatedAt =
                        DateTime.UtcNow
                };

            await context.FoodStalls
                .AddAsync(foodStall);

            await context.SaveChangesAsync();
        }


        var demoItems = new[]
        {
            new
            {
                Name =
                    "Chicken Burger",

                Description =
                    "Fresh chicken burger.",

                Price =
                    850.00m
            },

            new
            {
                Name =
                    "Veg Sandwich",

                Description =
                    "Fresh vegetarian sandwich.",

                Price =
                    600.00m
            },

            new
            {
                Name =
                    "Bottled Water",

                Description =
                    "Chilled bottled drinking water.",

                Price =
                    150.00m
            }
        };


        foreach (var item in demoItems)
        {
            var exists =
                await context.FoodItems
                    .AnyAsync(
                        foodItem =>
                            foodItem.FoodStallId ==
                            foodStall.Id &&
                            foodItem.Name ==
                            item.Name);

            if (exists)
            {
                continue;
            }

            var foodItem =
                new FoodItem
                {
                    FoodStallId =
                        foodStall.Id,

                    Name =
                        item.Name,

                    Description =
                        item.Description,

                    Price =
                        item.Price,

                    IsAvailable =
                        true,

                    CreatedAt =
                        DateTime.UtcNow
                };

            await context.FoodItems
                .AddAsync(foodItem);
        }

        await context.SaveChangesAsync();
    }


    // =========================================================
    // VENUE FACILITIES
    // =========================================================

    private static async Task SeedVenueFacilitiesAsync(
        ApplicationDbContext context,
        int venueId)
    {
        var facilities =
            new[]
            {
                new VenueFacility
                {
                    VenueId =
                        venueId,

                    Name =
                        "First Aid Station",

                    FacilityType =
                        FacilityType.FirstAid,

                    Zone =
                        "Zone A",

                    Floor =
                        "Ground Floor",

                    Description =
                        "First aid assistance for event attendees.",

                    IsAccessible =
                        true,

                    Status =
                        FacilityStatus.Open,

                    Directions =
                        "Near the main entrance and information desk.",

                    CreatedAt =
                        DateTime.UtcNow
                },

                new VenueFacility
                {
                    VenueId =
                        venueId,

                    Name =
                        "Accessible Washroom",

                    FacilityType =
                        FacilityType.Washroom,

                    Zone =
                        "Zone B",

                    Floor =
                        "Ground Floor",

                    Description =
                        "Accessible washroom for event attendees.",

                    IsAccessible =
                        true,

                    Status =
                        FacilityStatus.Open,

                    Directions =
                        "Beside Gate B.",

                    CreatedAt =
                        DateTime.UtcNow
                },

                new VenueFacility
                {
                    VenueId =
                        venueId,

                    Name =
                        "Information Desk",

                    FacilityType =
                        FacilityType.InformationDesk,

                    Zone =
                        "Main Entrance",

                    Floor =
                        "Ground Floor",

                    Description =
                        "Event information and attendee assistance.",

                    IsAccessible =
                        true,

                    Status =
                        FacilityStatus.Open,

                    Directions =
                        "Immediately inside the main entrance.",

                    CreatedAt =
                        DateTime.UtcNow
                }
            };


        foreach (var facility in facilities)
        {
            var exists =
                await context.VenueFacilities
                    .AnyAsync(
                        existingFacility =>
                            existingFacility.VenueId ==
                            venueId &&
                            existingFacility.Name ==
                            facility.Name);

            if (exists)
            {
                continue;
            }

            await context.VenueFacilities
                .AddAsync(facility);
        }

        await context.SaveChangesAsync();
    }
}