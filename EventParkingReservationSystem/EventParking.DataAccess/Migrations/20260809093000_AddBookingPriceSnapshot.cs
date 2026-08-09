using EventParking.DataAccess.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventParking.DataAccess.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260809093000_AddBookingPriceSnapshot")]
public partial class AddBookingPriceSnapshot : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "UnitPriceAtBooking",
            table: "BookingSeats",
            type: "decimal(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.Sql(
            """
            UPDATE bookingSeat
            SET bookingSeat.UnitPriceAtBooking =
                COALESCE(seat.PriceOverride, eventItem.TicketPrice)
            FROM BookingSeats AS bookingSeat
            INNER JOIN Seats AS seat
                ON seat.Id = bookingSeat.SeatId
            INNER JOIN Events AS eventItem
                ON eventItem.Id = seat.EventId;
            """);

        migrationBuilder.AddCheckConstraint(
            name:
                "CK_BookingSeats_UnitPriceAtBooking_NonNegative",
            table: "BookingSeats",
            sql: "[UnitPriceAtBooking] >= 0");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name:
                "CK_BookingSeats_UnitPriceAtBooking_NonNegative",
            table: "BookingSeats");

        migrationBuilder.DropColumn(
            name: "UnitPriceAtBooking",
            table: "BookingSeats");
    }
}
