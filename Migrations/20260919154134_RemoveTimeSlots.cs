using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace planeProject.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTimeSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_reservations_timeslot_TimeSlotId",
                table: "reservations");

            migrationBuilder.DropTable(
                name: "timeslot");

            migrationBuilder.DropIndex(
                name: "IX_reservations_HaircutId",
                table: "reservations");

            migrationBuilder.DropIndex(
                name: "IX_reservations_TimeSlotId",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "TimeSlotId",
                table: "reservations");

            migrationBuilder.AddColumn<DateTime>(
                name: "startTime",
                table: "reservations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "haircuts",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                table: "haircuts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_reservations_HaircutId",
                table: "reservations",
                column: "HaircutId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_reservations_HaircutId",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "startTime",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "haircuts");

            migrationBuilder.AddColumn<int>(
                name: "TimeSlotId",
                table: "reservations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<float>(
                name: "Price",
                table: "haircuts",
                type: "real",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.CreateTable(
                name: "timeslot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OpeningHoursId = table.Column<int>(type: "integer", nullable: false),
                    Slot = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_timeslot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_timeslot_openinghours_OpeningHoursId",
                        column: x => x.OpeningHoursId,
                        principalTable: "openinghours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_reservations_HaircutId",
                table: "reservations",
                column: "HaircutId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reservations_TimeSlotId",
                table: "reservations",
                column: "TimeSlotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_timeslot_OpeningHoursId",
                table: "timeslot",
                column: "OpeningHoursId");

            migrationBuilder.AddForeignKey(
                name: "FK_reservations_timeslot_TimeSlotId",
                table: "reservations",
                column: "TimeSlotId",
                principalTable: "timeslot",
                principalColumn: "Id");
        }
    }
}
