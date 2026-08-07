using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventParking.DataAccess.Configurations;

public class ParkingReservationConfiguration
    : IEntityTypeConfiguration<ParkingReservation>
{
    public void Configure(
        EntityTypeBuilder<ParkingReservation> builder)
    {
        builder.ToTable("ParkingReservations", table =>
        {
            table.HasCheckConstraint(
                "CK_ParkingReservations_FeeAtReservation_NonNegative",
                "[FeeAtReservation] >= 0");
        });

        builder.HasKey(reservation => reservation.Id);

        builder.Property(reservation => reservation.FeeAtReservation)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(reservation => reservation.CreatedAt)
            .IsRequired();

        builder.Property(reservation => reservation.UpdatedAt)
            .IsRequired(false);

        builder.HasOne(reservation => reservation.Booking)
            .WithOne(booking => booking.ParkingReservation)
            .HasForeignKey<ParkingReservation>(
                reservation => reservation.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(reservation => reservation.ParkingSlot)
            .WithMany(slot => slot.ParkingReservations)
            .HasForeignKey(reservation => reservation.ParkingSlotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(reservation => reservation.BookingId)
            .IsUnique()
            .HasDatabaseName(
                "UX_ParkingReservations_BookingId");

        builder.HasIndex(reservation => reservation.ParkingSlotId)
            .HasDatabaseName(
                "IX_ParkingReservations_ParkingSlotId");
    }
}
