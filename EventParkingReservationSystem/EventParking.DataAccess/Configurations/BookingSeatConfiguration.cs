using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventParking.DataAccess.Configurations;

public class BookingSeatConfiguration
    : IEntityTypeConfiguration<BookingSeat>
{
    public void Configure(
        EntityTypeBuilder<BookingSeat> builder)
    {
        builder.ToTable("BookingSeats", table =>
        {
            table.HasCheckConstraint(
                "CK_BookingSeats_UnitPriceAtBooking_NonNegative",
                "[UnitPriceAtBooking] >= 0");
        });

        builder.HasKey(bookingSeat => bookingSeat.Id);

        builder.Property(bookingSeat => bookingSeat.CreatedAt)
            .IsRequired();

        builder.Property(bookingSeat =>
                bookingSeat.UnitPriceAtBooking)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(bookingSeat => bookingSeat.UpdatedAt)
            .IsRequired(false);

        builder.HasOne(bookingSeat => bookingSeat.Booking)
            .WithMany(booking => booking.BookingSeats)
            .HasForeignKey(bookingSeat => bookingSeat.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(bookingSeat => bookingSeat.Seat)
            .WithMany(seat => seat.BookingSeats)
            .HasForeignKey(bookingSeat => bookingSeat.SeatId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(bookingSeat => new
        {
            bookingSeat.BookingId,
            bookingSeat.SeatId
        })
        .IsUnique()
        .HasDatabaseName(
            "UX_BookingSeats_BookingId_SeatId");

        builder.HasIndex(bookingSeat => bookingSeat.SeatId)
            .HasDatabaseName(
                "IX_BookingSeats_SeatId");

    }
}
