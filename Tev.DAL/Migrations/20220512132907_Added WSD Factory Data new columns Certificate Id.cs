using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class AddedWSDFactoryDatanewcolumnsCertificateId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CcDeviceVersion",
                table: "DeviceFactoryData",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CcTargetedVersion",
                table: "DeviceFactoryData",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertificateId",
                table: "DeviceFactoryData",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "DeviceFactoryData",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogicalDeviceId",
                table: "DeviceFactoryData",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MspDeviceVersion",
                table: "DeviceFactoryData",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MspTargetedVersion",
                table: "DeviceFactoryData",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CcDeviceVersion",
                table: "DeviceFactoryData");

            migrationBuilder.DropColumn(
                name: "CcTargetedVersion",
                table: "DeviceFactoryData");

            migrationBuilder.DropColumn(
                name: "CertificateId",
                table: "DeviceFactoryData");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "DeviceFactoryData");

            migrationBuilder.DropColumn(
                name: "LogicalDeviceId",
                table: "DeviceFactoryData");

            migrationBuilder.DropColumn(
                name: "MspDeviceVersion",
                table: "DeviceFactoryData");

            migrationBuilder.DropColumn(
                name: "MspTargetedVersion",
                table: "DeviceFactoryData");
        }
    }
}
