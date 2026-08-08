using EventParking.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventParking.DataAccess.Configurations;

public class VenueFacilityConfiguration
    : IEntityTypeConfiguration<VenueFacility>
{
    public void Configure(EntityTypeBuilder<VenueFacility> builder)
    {
        builder.ToTable("VenueFacilities");

        builder.HasKey(facility => facility.Id);

        builder.Property(facility => facility.VenueId)
            .IsRequired();

        builder.Property(facility => facility.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(facility => facility.FacilityType)
            .IsRequired();

        builder.Property(facility => facility.Zone)
            .HasMaxLength(100);

        builder.Property(facility => facility.Floor)
            .HasMaxLength(100);

        builder.Property(facility => facility.Description)
            .HasMaxLength(500);

        builder.Property(facility => facility.IsAccessible)
            .IsRequired();

        builder.Property(facility => facility.Status)
            .IsRequired();

        builder.Property(facility => facility.Directions)
            .HasMaxLength(500);

        builder.Property(facility => facility.CreatedAt)
            .IsRequired();

        builder.Property(facility => facility.UpdatedAt)
            .IsRequired(false);

        builder.HasOne(facility => facility.Venue)
            .WithMany(venue => venue.Facilities)
            .HasForeignKey(facility => facility.VenueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(facility => facility.VenueId)
            .HasDatabaseName("IX_VenueFacilities_VenueId");

        builder.HasIndex(facility => facility.FacilityType)
            .HasDatabaseName("IX_VenueFacilities_FacilityType");

        builder.HasIndex(facility => facility.Status)
            .HasDatabaseName("IX_VenueFacilities_Status");
    }
}