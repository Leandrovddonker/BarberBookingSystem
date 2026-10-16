using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberBookingSystem.Migrations
{
    /// <inheritdoc />
    public partial class mappedBarberWithReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_reservations_barbers_BarberId",
                table: "reservations");

            migrationBuilder.RenameColumn(
                name: "startTime",
                table: "reservations",
                newName: "StartTime");

            migrationBuilder.AddForeignKey(
                name: "FK_reservations_barbers_BarberId",
                table: "reservations",
                column: "BarberId",
                principalTable: "barbers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_reservations_barbers_BarberId",
                table: "reservations");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "reservations",
                newName: "startTime");

            migrationBuilder.AddForeignKey(
                name: "FK_reservations_barbers_BarberId",
                table: "reservations",
                column: "BarberId",
                principalTable: "barbers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
