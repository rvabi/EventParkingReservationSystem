using EventParking.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventParking.DataAccess.Configurations;

public class FoodStallConfiguration
    : IEntityTypeConfiguration<FoodStall>
{
    public void Configure(EntityTypeBuilder<FoodStall> builder)
    {
        builder.ToTable("FoodStalls");

        builder.HasKey(foodStall => foodStall.Id);

        builder.Property(foodStall => foodStall.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(foodStall => foodStall.Description)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(foodStall => foodStall.Status)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(foodStall => foodStall.CreatedAt)
            .IsRequired();

        builder.Property(foodStall => foodStall.UpdatedAt)
            .IsRequired(false);

        builder.HasOne(foodStall => foodStall.Event)
            .WithMany()
            .HasForeignKey(foodStall => foodStall.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(foodStall => foodStall.EventId)
            .HasDatabaseName(
                "IX_FoodStalls_EventId");

        builder.HasIndex(foodStall => new
        {
            foodStall.EventId,
            foodStall.Status
        })
        .HasDatabaseName(
            "IX_FoodStalls_EventId_Status");
    }
}
