using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using CityMixedGolf.Web.Data;

#nullable disable

namespace CityMixedGolf.Web.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260703210000_DropLegacyAspNetUsersColumns")]
    public partial class DropLegacyAspNetUsersColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Gender, HandicapIndex and BandColour used to live on AspNetUsers.
            // They are now computed from GolfPlayerRecord (Gender/HandicapIndex)
            // or per-competition on CompetitionEntry (BandColour), but the old
            // NOT NULL columns were left behind, breaking new user inserts.
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'Gender'
                )
                BEGIN
                    ALTER TABLE AspNetUsers DROP COLUMN Gender;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'HandicapIndex'
                )
                BEGIN
                    ALTER TABLE AspNetUsers DROP COLUMN HandicapIndex;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'BandColour'
                )
                BEGIN
                    ALTER TABLE AspNetUsers DROP COLUMN BandColour;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'Gender'
                )
                BEGIN
                    ALTER TABLE AspNetUsers ADD Gender INT NOT NULL DEFAULT 0;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'HandicapIndex'
                )
                BEGIN
                    ALTER TABLE AspNetUsers ADD HandicapIndex DECIMAL(5,1) NOT NULL DEFAULT 0;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'BandColour'
                )
                BEGIN
                    ALTER TABLE AspNetUsers ADD BandColour INT NOT NULL DEFAULT 0;
                END
            ");
        }
    }
}
