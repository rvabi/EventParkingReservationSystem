using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventParking.DataAccess.Configurations;

public class PaymentConfiguration
    : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments", table =>
        {
            table.HasCheckConstraint(
                "CK_Payments_Amount_NonNegative",
                "[Amount] >= 0");
        });

        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(payment => payment.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(payment => payment.TransactionReference)
            .HasMaxLength(100);

        builder.Property(payment => payment.PaidAt)
            .IsRequired(false);

        builder.Property(payment => payment.CreatedAt)
            .IsRequired();

        builder.Property(payment => payment.UpdatedAt)
            .IsRequired(false);

        builder.HasOne(payment => payment.Booking)
            .WithOne(booking => booking.Payment)
            .HasForeignKey<Payment>(
                payment => payment.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(payment => payment.Customer)
            .WithMany(customer => customer.Payments)
            .HasForeignKey(payment => payment.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(payment => payment.BookingId)
            .IsUnique()
            .HasDatabaseName("UX_Payments_BookingId");

        builder.HasIndex(payment => new
        {
            payment.CustomerId,
            payment.PaidAt
        })
        .HasDatabaseName(
            "IX_Payments_CustomerId_PaidAt");
    }
}
