using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventParking.DataAccess.Migrations
{
    public partial class AddParkingFeeOverride : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No operation.
            // FeeOverride was already added by AddFeeOverrideToParkingSlots.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No operation.
        }
    }
}