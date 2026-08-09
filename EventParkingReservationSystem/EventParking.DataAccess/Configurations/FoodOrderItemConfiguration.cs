using EventParking.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventParking.DataAccess.Configurations;

public class FoodOrderItemConfiguration
    : IEntityTypeConfiguration<FoodOrderItem>
{
    public void Configure(EntityTypeBuilder<FoodOrderItem> builder)
    {
        builder.ToTable("FoodOrderItems", table =>
        {
            table.HasCheckConstraint(
                "CK_FoodOrderItems_Quantity_Positive",
                "[Quantity] > 0");

            table.HasCheckConstraint(
                "CK_FoodOrderItems_UnitPrice_NonNegative",
                "[UnitPrice] >= 0");

            table.HasCheckConstraint(
                "CK_FoodOrderItems_LineTotal_NonNegative",
                "[LineTotal] >= 0");
        });

        builder.HasKey(foodOrderItem => foodOrderItem.Id);

        builder.Property(foodOrderItem => foodOrderItem.Quantity)
            .IsRequired();

        builder.Property(foodOrderItem => foodOrderItem.UnitPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(foodOrderItem => foodOrderItem.LineTotal)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(foodOrderItem => foodOrderItem.CreatedAt)
            .IsRequired();

        builder.Property(foodOrderItem => foodOrderItem.UpdatedAt)
            .IsRequired(false);

        builder.HasOne(foodOrderItem => foodOrderItem.FoodOrder)
            .WithMany(foodOrder => foodOrder.Items)
            .HasForeignKey(foodOrderItem => foodOrderItem.FoodOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(foodOrderItem => foodOrderItem.FoodItem)
            .WithMany(foodItem => foodItem.FoodOrderItems)
            .HasForeignKey(foodOrderItem => foodOrderItem.FoodItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(foodOrderItem => foodOrderItem.FoodOrderId)
            .HasDatabaseName(
                "IX_FoodOrderItems_FoodOrderId");

        builder.HasIndex(foodOrderItem => foodOrderItem.FoodItemId)
            .HasDatabaseName(
                "IX_FoodOrderItems_FoodItemId");
    }
}