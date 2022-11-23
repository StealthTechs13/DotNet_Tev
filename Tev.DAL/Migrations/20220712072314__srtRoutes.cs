using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class _srtRoutes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SRTRoutes",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    CreatedDate = table.Column<long>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    ModifiedDate = table.Column<long>(nullable: true),
                    ModifiedBy = table.Column<string>(nullable: true),
                    RouteId = table.Column<string>(nullable: true),
                    Des_Port = table.Column<int>(nullable: false),
                    Sor_Port = table.Column<int>(nullable: false),
                    PassPhrase = table.Column<string>(nullable: true),
                    GatewayIP = table.Column<string>(nullable: true),
                    Des_PortName = table.Column<string>(nullable: true),
                    Sor_PortName = table.Column<string>(nullable: true),
                    State = table.Column<string>(nullable: true),
                    DeviceId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SRTRoutes", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SRTRoutes");
        }
    }
}
