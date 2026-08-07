using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventParking.DataAccess.Configurations;

public class BookingConfiguration
    : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings", table =>
        {
            table.HasCheckConstraint(
                "CK_Bookings_TotalAmount_NonNegative",
                "[TotalAmount] >= 0");
        });

        builder.HasKey(booking => booking.Id);

        builder.Property(booking => booking.BookingNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(booking => booking.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(booking => booking.HoldExpiresAt)
            .IsRequired(false);

        builder.Property(booking => booking.TotalAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(booking => booking.CancelledAt)
            .IsRequired(false);

        builder.Property(booking => booking.CreatedAt)
            .IsRequired();

        builder.Property(booking => booking.UpdatedAt)
            .IsRequired(false);

        builder.HasOne(booking => booking.Customer)
            .WithMany(customer => customer.Bookings)
            .HasForeignKey(booking => booking.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(booking => booking.Event)
            .WithMany(eventItem => eventItem.Bookings)
            .HasForeignKey(booking => booking.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(booking => booking.BookingNumber)
            .IsUnique()
            .HasDatabaseName(
                "UX_Bookings_BookingNumber");

        builder.HasIndex(booking => new
        {
            booking.CustomerId,
            booking.CreatedAt
        })
        .HasDatabaseName(
            "IX_Bookings_CustomerId_CreatedAt");

        builder.HasIndex(booking => new
        {
            booking.Status,
            booking.HoldExpiresAt
        })
        .HasDatabaseName(
            "IX_Bookings_Status_HoldExpiresAt");
    }
}
