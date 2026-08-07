using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.DataAccess.Context;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventParking.DataAccess.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await SeedUsersAsync(context);
        await SeedEventDataAsync(context);
    }

    private static async Task SeedUsersAsync(
        ApplicationDbContext context)
    {
        const string adminEmail = "admin@eventparking.local";

        if (!await context.Customers.AnyAsync(
                customer => customer.Email == adminEmail))
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
                BCrypt.Net.BCrypt.HashPassword("Admin@12345");

            await context.Customers.AddAsync(admin);
        }

        const string customerEmail =
            "customer@eventparking.local";

        if (!await context.Customers.AnyAsync(
                customer => customer.Email == customerEmail))
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
                BCrypt.Net.BCrypt.HashPassword("Customer@12345");

            await context.Customers.AddAsync(customer);
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedEventDataAsync(
        ApplicationDbContext context)
    {
        var venue = await context.Venues
            .FirstOrDefaultAsync(
                venue => venue.Name == "Vavuniya Event Centre");

        if (venue is null)
        {
            venue = new Venue
            {
                Name = "Vavuniya Event Centre",
                Address = "Vavuniya, Sri Lanka",
                TotalCapacity = 500,
                CreatedAt = DateTime.UtcNow
            };

            await context.Venues.AddAsync(venue);
            await context.SaveChangesAsync();
        }

        var category = await context.EventCategories
            .FirstOrDefaultAsync(
                category => category.Name == "Technology");

        if (category is null)
        {
            category = new EventCategory
            {
                Name = "Technology",
                Description =
                    "Technology conferences and exhibitions.",
                CreatedAt = DateTime.UtcNow
            };

            await context.EventCategories.AddAsync(category);
            await context.SaveChangesAsync();
        }

        var eventItem = await context.Events
            .FirstOrDefaultAsync(
                eventItem =>
                    eventItem.Name == "Smart Technology Expo");

        if (eventItem is null)
        {
            var eventStart =
                DateTime.UtcNow.Date.AddDays(30).AddHours(9);

            eventItem = new Event
            {
                Name = "Smart Technology Expo",
                Description =
                    "Demo technology event for the reservation system.",
                VenueId = venue.Id,
                EventCategoryId = category.Id,
                StartDateTime = eventStart,
                EndDateTime = eventStart.AddHours(6),
                TicketPrice = 2500.00m,
                ParkingFee = 500.00m,
                Capacity = 100,
                CreatedAt = DateTime.UtcNow
            };

            await context.Events.AddAsync(eventItem);
            await context.SaveChangesAsync();
        }

        await SeedSeatsAsync(context, eventItem.Id);
        await SeedParkingSlotsAsync(context, eventItem.Id);
    }

    private static async Task SeedSeatsAsync(
        ApplicationDbContext context,
        int eventId)
    {
        if (await context.Seats.AnyAsync(
                seat => seat.EventId == eventId))
        {
            return;
        }

        var seats = new List<Seat>();

        for (var number = 1; number <= 10; number++)
        {
            seats.Add(new Seat
            {
                EventId = eventId,
                SeatNumber = $"A{number}",
                RowLabel = "A",
                ColumnNumber = number,
                SeatType = "Standard",
                Status = SeatStatus.Available,
                CreatedAt = DateTime.UtcNow
            });
        }

        await context.Seats.AddRangeAsync(seats);
        await context.SaveChangesAsync();
    }

    private static async Task SeedParkingSlotsAsync(
        ApplicationDbContext context,
        int eventId)
    {
        if (await context.ParkingSlots.AnyAsync(
                slot => slot.EventId == eventId))
        {
            return;
        }

        var parkingSlots = new List<ParkingSlot>();

        for (var number = 1; number <= 5; number++)
        {
            parkingSlots.Add(new ParkingSlot
            {
                EventId = eventId,
                SlotNumber = $"P{number:00}",
                Zone = "Zone A",
                Status = ParkingSlotStatus.Available,
                CreatedAt = DateTime.UtcNow
            });
        }

        await context.ParkingSlots.AddRangeAsync(parkingSlots);
        await context.SaveChangesAsync();
    }
}