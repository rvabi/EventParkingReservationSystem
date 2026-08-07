using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventParking.DataAccess.Configurations;

public class CustomerConfiguration
    : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(customer => customer.Id);

        builder.Property(customer => customer.FullName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(customer => customer.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(customer => customer.Phone)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(customer => customer.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(customer => customer.Role)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(customer => customer.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(customer => customer.EmailVerified)
            .IsRequired();

        builder.Property(customer => customer.EmailVerificationTokenHash)
            .HasMaxLength(500);

        builder.Property(customer => customer.PasswordResetTokenHash)
            .HasMaxLength(500);

        builder.Property(customer => customer.CreatedAt)
            .IsRequired();

        builder.Property(customer => customer.UpdatedAt)
            .IsRequired(false);

        builder.HasIndex(customer => customer.Email)
            .IsUnique()
            .HasDatabaseName("UX_Customers_Email");
    }
}
