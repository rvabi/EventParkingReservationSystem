using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventParking.DataAccess.Configurations;

public class ParkingSlotConfiguration
    : IEntityTypeConfiguration<ParkingSlot>
{
    public void Configure(EntityTypeBuilder<ParkingSlot> builder)
    {
        builder.ToTable("ParkingSlots");

        builder.HasKey(slot => slot.Id);

        builder.Property(slot => slot.SlotNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(slot => slot.Zone)
            .HasMaxLength(50);

        builder.Property(slot => slot.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(slot => slot.CreatedAt)
            .IsRequired();

        builder.Property(slot => slot.UpdatedAt)
            .IsRequired(false);

        builder.HasOne(slot => slot.Event)
            .WithMany(eventItem => eventItem.ParkingSlots)
            .HasForeignKey(slot => slot.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(slot => new
        {
            slot.EventId,
            slot.SlotNumber
        })
        .IsUnique()
        .HasDatabaseName(
            "UX_ParkingSlots_EventId_SlotNumber");

        builder.HasIndex(slot => new
        {
            slot.EventId,
            slot.Status
        })
        .HasDatabaseName(
            "IX_ParkingSlots_EventId_Status");
    }
}
