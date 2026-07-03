using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using CityMixedGolf.Web.Data;

#nullable disable

namespace CityMixedGolf.Web.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20250630000003_BandColourOnEntry")]
    public partial class BandColourOnEntry : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add BandColour to CompetitionEntries
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'CompetitionEntries' AND COLUMN_NAME = 'BandColour'
                )
                BEGIN
                    ALTER TABLE CompetitionEntries ADD BandColour INT NOT NULL DEFAULT 2;
                END
            ");

            // Remove BandColour from GolfPlayerRecords if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'GolfPlayerRecords' AND COLUMN_NAME = 'BandColour'
                )
                BEGIN
                    ALTER TABLE GolfPlayerRecords DROP COLUMN BandColour;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'CompetitionEntries' AND COLUMN_NAME = 'BandColour'
                )
                BEGIN
                    ALTER TABLE CompetitionEntries DROP COLUMN BandColour;
                END
            ");
        }
    }
}