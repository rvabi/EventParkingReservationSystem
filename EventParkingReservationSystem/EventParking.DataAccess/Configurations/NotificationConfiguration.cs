using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventParking.DataAccess.Configurations;

public class NotificationConfiguration
    : IEntityTypeConfiguration<Notification>
{
    public void Configure(
        EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(notification => notification.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(notification => notification.Message)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(notification => notification.IsRead)
            .IsRequired();

        builder.Property(notification => notification.ReadAt)
            .IsRequired(false);

        builder.Property(notification => notification.CreatedAt)
            .IsRequired();

        builder.Property(notification => notification.UpdatedAt)
            .IsRequired(false);

        builder.HasOne(notification => notification.Customer)
            .WithMany(customer => customer.Notifications)
            .HasForeignKey(notification => notification.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(notification => new
        {
            notification.CustomerId,
            notification.IsRead,
            notification.CreatedAt
        })
        .HasDatabaseName(
            "IX_Notifications_Customer_Read_Created");
    }
}
