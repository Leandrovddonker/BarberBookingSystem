using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberBookingSystem.Migrations
{
    public partial class ChangeOpeningHoursToTimeOnly : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE openinghours
                    ALTER COLUMN "OpeningTime" TYPE time without time zone USING "OpeningTime"::time without time zone,
                    ALTER COLUMN "ClosingTime" TYPE time without time zone USING "ClosingTime"::time without time zone;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE openinghours
                    ALTER COLUMN "OpeningTime" TYPE interval USING "OpeningTime"::interval,
                    ALTER COLUMN "ClosingTime" TYPE interval USING "ClosingTime"::interval;
                """);
        }
    }
}
