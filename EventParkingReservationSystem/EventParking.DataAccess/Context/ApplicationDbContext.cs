using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventParking.DataAccess.Context;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; } = null!;

    public DbSet<Venue> Venues { get; set; } = null!;

    public DbSet<EventCategory> EventCategories { get; set; } = null!;

    public DbSet<Event> Events { get; set; } = null!;

    public DbSet<Seat> Seats { get; set; } = null!;

    public DbSet<ParkingSlot> ParkingSlots { get; set; } = null!;

    public DbSet<Booking> Bookings { get; set; } = null!;

    public DbSet<BookingSeat> BookingSeats { get; set; } = null!;

    public DbSet<ParkingReservation> ParkingReservations { get; set; } = null!;

    public DbSet<Payment> Payments { get; set; } = null!;

    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<FoodStall> FoodStalls { get; set; } = null!;

    public DbSet<FoodItem> FoodItems { get; set; } = null!;

    public DbSet<FoodOrder> FoodOrders { get; set; } = null!;

    public DbSet<FoodOrderItem> FoodOrderItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }
}