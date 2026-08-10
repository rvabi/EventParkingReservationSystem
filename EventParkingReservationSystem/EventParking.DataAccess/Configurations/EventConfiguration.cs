using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventParking.DataAccess.Configurations;

public class EventConfiguration
    : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events", table =>
        {
            table.HasCheckConstraint(
                "CK_Events_Capacity_Positive",
                "[Capacity] > 0");

            table.HasCheckConstraint(
                "CK_Events_TicketPrice_NonNegative",
                "[TicketPrice] >= 0");

            table.HasCheckConstraint(
                "CK_Events_ParkingFee_NonNegative",
                "[ParkingFee] >= 0");

            table.HasCheckConstraint(
                "CK_Events_EndDateTime_After_StartDateTime",
                "[EndDateTime] > [StartDateTime]");
        });

        builder.HasKey(eventItem => eventItem.Id);

        builder.Property(eventItem => eventItem.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(eventItem => eventItem.Description)
            .HasMaxLength(2000);

        builder.Property(eventItem => eventItem.StartDateTime)
            .IsRequired();

        builder.Property(eventItem => eventItem.EndDateTime)
            .IsRequired();

        builder.Property(eventItem => eventItem.TicketPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(eventItem => eventItem.ParkingFee)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(eventItem => eventItem.Capacity)
            .IsRequired();

        builder.Property(eventItem => eventItem.SeatingLayoutType)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(SeatingLayoutType.StraightRows);

        builder.Property(eventItem => eventItem.CreatedAt)
            .IsRequired();

        builder.Property(eventItem => eventItem.UpdatedAt)
            .IsRequired(false);

        builder.HasOne(eventItem => eventItem.Venue)
            .WithMany(venue => venue.Events)
            .HasForeignKey(eventItem => eventItem.VenueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(eventItem => eventItem.EventCategory)
            .WithMany(category => category.Events)
            .HasForeignKey(eventItem => eventItem.EventCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(eventItem => eventItem.Name)
            .HasDatabaseName("IX_Events_Name");

        builder.HasIndex(eventItem => new
        {
            eventItem.VenueId,
            eventItem.StartDateTime,
            eventItem.EndDateTime
        })
        .HasDatabaseName("IX_Events_Venue_DateTime");
    }
}
