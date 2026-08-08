using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventParking.DataAccess.Configurations;

public class SeatConfiguration
    : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.ToTable("Seats", table =>
        {
            table.HasCheckConstraint(
                "CK_Seats_PriceOverride_NonNegative",
                "[PriceOverride] IS NULL OR [PriceOverride] >= 0");
        });

        builder.HasKey(seat => seat.Id);

        builder.Property(seat => seat.SeatNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(seat => seat.RowLabel)
            .HasMaxLength(20);

        builder.Property(seat => seat.ColumnNumber)
            .IsRequired(false);

        builder.Property(seat => seat.SeatType)
            .HasMaxLength(50);

        builder.Property(seat => seat.PriceOverride)
            .HasColumnType("decimal(18,2)")
            .IsRequired(false);

        builder.Property(seat => seat.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(seat => seat.CreatedAt)
            .IsRequired();

        builder.Property(seat => seat.UpdatedAt)
            .IsRequired(false);

        builder.HasOne(seat => seat.Event)
            .WithMany(eventItem => eventItem.Seats)
            .HasForeignKey(seat => seat.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(seat => new
        {
            seat.EventId,
            seat.SeatNumber
        })
        .IsUnique()
        .HasDatabaseName("UX_Seats_EventId_SeatNumber");

        builder.HasIndex(seat => new
        {
            seat.EventId,
            seat.Status
        })
        .HasDatabaseName("IX_Seats_EventId_Status");
    }
}
