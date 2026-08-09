using EventParking.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventParking.DataAccess.Configurations;

public class FoodOrderConfiguration
    : IEntityTypeConfiguration<FoodOrder>
{
    public void Configure(EntityTypeBuilder<FoodOrder> builder)
    {
        builder.ToTable("FoodOrders", table =>
        {
            table.HasCheckConstraint(
                "CK_FoodOrders_TotalAmount_NonNegative",
                "[TotalAmount] >= 0");
        });

        builder.HasKey(foodOrder => foodOrder.Id);

        builder.Property(foodOrder => foodOrder.OrderNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(foodOrder => foodOrder.PickupTime)
            .IsRequired();

        builder.Property(foodOrder => foodOrder.TotalAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(foodOrder => foodOrder.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(foodOrder => foodOrder.CreatedAt)
            .IsRequired();

        builder.Property(foodOrder => foodOrder.UpdatedAt)
            .IsRequired(false);

        builder.HasOne(foodOrder => foodOrder.Booking)
            .WithMany()
            .HasForeignKey(foodOrder => foodOrder.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(foodOrder => foodOrder.Customer)
            .WithMany()
            .HasForeignKey(foodOrder => foodOrder.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(foodOrder => foodOrder.FoodStall)
            .WithMany(foodStall => foodStall.FoodOrders)
            .HasForeignKey(foodOrder => foodOrder.FoodStallId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(foodOrder => foodOrder.OrderNumber)
            .IsUnique()
            .HasDatabaseName(
                "UX_FoodOrders_OrderNumber");

        builder.HasIndex(foodOrder => new
        {
            foodOrder.CustomerId,
            foodOrder.CreatedAt
        })
        .HasDatabaseName(
            "IX_FoodOrders_CustomerId_CreatedAt");

        builder.HasIndex(foodOrder => new
        {
            foodOrder.FoodStallId,
            foodOrder.Status
        })
        .HasDatabaseName(
            "IX_FoodOrders_FoodStallId_Status");

        builder.HasIndex(foodOrder => foodOrder.BookingId)
            .HasDatabaseName(
                "IX_FoodOrders_BookingId");
    }
}
