using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class sdcardhistorysize : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "size",
                table: "SDCardHistory",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "size",
                table: "SDCardHistory");
        }
    }
}
