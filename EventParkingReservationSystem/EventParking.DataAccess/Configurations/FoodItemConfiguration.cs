using EventParking.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventParking.DataAccess.Configurations;

public class FoodItemConfiguration
    : IEntityTypeConfiguration<FoodItem>
{
    public void Configure(EntityTypeBuilder<FoodItem> builder)
    {
        builder.ToTable("FoodItems", table =>
        {
            table.HasCheckConstraint(
                "CK_FoodItems_Price_NonNegative",
                "[Price] >= 0");
        });

        builder.HasKey(foodItem => foodItem.Id);

        builder.Property(foodItem => foodItem.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(foodItem => foodItem.Description)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(foodItem => foodItem.Price)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(foodItem => foodItem.IsAvailable)
            .IsRequired();

        builder.Property(foodItem => foodItem.CreatedAt)
            .IsRequired();

        builder.Property(foodItem => foodItem.UpdatedAt)
            .IsRequired(false);

        builder.HasOne(foodItem => foodItem.FoodStall)
            .WithMany(foodStall => foodStall.FoodItems)
            .HasForeignKey(foodItem => foodItem.FoodStallId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(foodItem => new
        {
            foodItem.FoodStallId,
            foodItem.IsAvailable
        })
        .HasDatabaseName(
            "IX_FoodItems_FoodStallId_IsAvailable");
    }
}