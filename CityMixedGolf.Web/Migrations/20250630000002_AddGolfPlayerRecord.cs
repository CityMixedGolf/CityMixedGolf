using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CityMixedGolf.Web.Migrations
{
    public partial class AddGolfPlayerRecord : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GolfPlayerRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HandicapIndex = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    BandColour = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_GolfPlayerRecords", x => x.Id));

            // Add GolfPlayerRecordId FK to AspNetUsers
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'GolfPlayerRecordId'
                )
                BEGIN
                    ALTER TABLE AspNetUsers ADD GolfPlayerRecordId INT NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_AspNetUsers_GolfPlayerRecordId'
                    AND object_id = OBJECT_ID('AspNetUsers')
                )
                BEGIN
                    CREATE UNIQUE INDEX IX_AspNetUsers_GolfPlayerRecordId
                    ON AspNetUsers (GolfPlayerRecordId)
                    WHERE GolfPlayerRecordId IS NOT NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = 'FK_AspNetUsers_GolfPlayerRecords_GolfPlayerRecordId'
                )
                BEGIN
                    ALTER TABLE AspNetUsers
                    ADD CONSTRAINT FK_AspNetUsers_GolfPlayerRecords_GolfPlayerRecordId
                    FOREIGN KEY (GolfPlayerRecordId) REFERENCES GolfPlayerRecords(Id)
                    ON DELETE SET NULL;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AspNetUsers_GolfPlayerRecords_GolfPlayerRecordId')
                    ALTER TABLE AspNetUsers DROP CONSTRAINT FK_AspNetUsers_GolfPlayerRecords_GolfPlayerRecordId;
            ");
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AspNetUsers_GolfPlayerRecordId' AND object_id = OBJECT_ID('AspNetUsers'))
                    DROP INDEX IX_AspNetUsers_GolfPlayerRecordId ON AspNetUsers;
            ");
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'GolfPlayerRecordId')
                    ALTER TABLE AspNetUsers DROP COLUMN GolfPlayerRecordId;
            ");
            migrationBuilder.DropTable(name: "GolfPlayerRecords");
        }
    }
}