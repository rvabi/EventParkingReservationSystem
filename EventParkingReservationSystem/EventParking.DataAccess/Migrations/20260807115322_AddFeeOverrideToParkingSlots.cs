using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventParking.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddFeeOverrideToParkingSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FeeOverride",
                table: "ParkingSlots",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ParkingSlots_FeeOverride_NonNegative",
                table: "ParkingSlots",
                sql: "[FeeOverride] IS NULL OR [FeeOverride] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ParkingSlots_FeeOverride_NonNegative",
                table: "ParkingSlots");

            migrationBuilder.DropColumn(
                name: "FeeOverride",
                table: "ParkingSlots");
        }
    }
}
