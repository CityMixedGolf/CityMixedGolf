using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using CityMixedGolf.Web.Data;

#nullable disable

namespace CityMixedGolf.Web.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20250630000001_AddUsualPartnerId")]
    public partial class AddUsualPartnerId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Column and index are applied manually via SSMS if not already present.
            // This migration records the change in __EFMigrationsHistory only.
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'UsualPartnerId'
                )
                BEGIN
                    ALTER TABLE AspNetUsers ADD UsualPartnerId NVARCHAR(450) NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_AspNetUsers_UsualPartnerId'
                    AND object_id = OBJECT_ID('AspNetUsers')
                )
                BEGIN
                    CREATE INDEX IX_AspNetUsers_UsualPartnerId ON AspNetUsers (UsualPartnerId);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = 'FK_AspNetUsers_AspNetUsers_UsualPartnerId'
                )
                BEGIN
                    ALTER TABLE AspNetUsers
                    ADD CONSTRAINT FK_AspNetUsers_AspNetUsers_UsualPartnerId
                    FOREIGN KEY (UsualPartnerId) REFERENCES AspNetUsers(Id)
                    ON DELETE NO ACTION;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AspNetUsers_AspNetUsers_UsualPartnerId')
                    ALTER TABLE AspNetUsers DROP CONSTRAINT FK_AspNetUsers_AspNetUsers_UsualPartnerId;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AspNetUsers_UsualPartnerId' AND object_id = OBJECT_ID('AspNetUsers'))
                    DROP INDEX IX_AspNetUsers_UsualPartnerId ON AspNetUsers;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'UsualPartnerId')
                    ALTER TABLE AspNetUsers DROP COLUMN UsualPartnerId;
            ");
        }
    }
}