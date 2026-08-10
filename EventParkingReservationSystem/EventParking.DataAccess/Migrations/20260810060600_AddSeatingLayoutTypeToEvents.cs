using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventParking.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSeatingLayoutTypeToEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SeatingLayoutType",
                table: "Events",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeatingLayoutType",
                table: "Events");
        }
    }
}
