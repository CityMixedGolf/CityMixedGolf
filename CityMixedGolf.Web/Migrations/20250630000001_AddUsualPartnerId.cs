using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CityMixedGolf.Web.Migrations
{
    public partial class AddUsualPartnerId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UsualPartnerId",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_UsualPartnerId",
                table: "AspNetUsers",
                column: "UsualPartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_UsualPartnerId",
                table: "AspNetUsers",
                column: "UsualPartnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_UsualPartnerId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_UsualPartnerId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "UsualPartnerId",
                table: "AspNetUsers");
        }
    }
}