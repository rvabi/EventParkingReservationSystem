using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventParking.DataAccess.Configurations;

public class VenueConfiguration
    : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.ToTable("Venues", table =>
        {
            table.HasCheckConstraint(
                "CK_Venues_TotalCapacity_Positive",
                "[TotalCapacity] > 0");
        });

        builder.HasKey(venue => venue.Id);

        builder.Property(venue => venue.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(venue => venue.Address)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(venue => venue.TotalCapacity)
            .IsRequired();

        builder.Property(venue => venue.CreatedAt)
            .IsRequired();

        builder.Property(venue => venue.UpdatedAt)
            .IsRequired(false);

        builder.HasIndex(venue => venue.Name)
            .HasDatabaseName("IX_Venues_Name");
    }
}
