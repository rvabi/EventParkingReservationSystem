using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventParking.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddParkingReservationActiveState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
     name: "IX_ParkingReservations_ParkingSlotId",
     table: "ParkingReservations");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ParkingReservations",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "UX_ParkingReservations_ParkingSlotId_Active",
                table: "ParkingReservations",
                column: "ParkingSlotId",
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
     name: "UX_ParkingReservations_ParkingSlotId_Active",
     table: "ParkingReservations");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ParkingReservations");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingReservations_ParkingSlotId",
                table: "ParkingReservations",
                column: "ParkingSlotId");
        }
    }
}
