using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class AddedWSDFactoryDatanewcolumnsFinal : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "DeviceFactoryData");

            migrationBuilder.RenameColumn(
                name: "MspTargetedVersion",
                table: "DeviceFactoryData",
                newName: "MspTargetedversion");

            migrationBuilder.RenameColumn(
                name: "MspDeviceVersion",
                table: "DeviceFactoryData",
                newName: "MspDeviceversion");

            migrationBuilder.RenameColumn(
                name: "LogicalDeviceId",
                table: "DeviceFactoryData",
                newName: "LogicalDeviceID");

            migrationBuilder.RenameColumn(
                name: "CertificateId",
                table: "DeviceFactoryData",
                newName: "CertificateID");

            migrationBuilder.RenameColumn(
                name: "CcTargetedVersion",
                table: "DeviceFactoryData",
                newName: "CcTargetedversion");

            migrationBuilder.RenameColumn(
                name: "CcDeviceVersion",
                table: "DeviceFactoryData",
                newName: "CcDeviceversion");

            migrationBuilder.AddColumn<string>(
                name: "FailureReasons",
                table: "DeviceFactoryData",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailureReasons",
                table: "DeviceFactoryData");

            migrationBuilder.RenameColumn(
                name: "MspTargetedversion",
                table: "DeviceFactoryData",
                newName: "MspTargetedVersion");

            migrationBuilder.RenameColumn(
                name: "MspDeviceversion",
                table: "DeviceFactoryData",
                newName: "MspDeviceVersion");

            migrationBuilder.RenameColumn(
                name: "LogicalDeviceID",
                table: "DeviceFactoryData",
                newName: "LogicalDeviceId");

            migrationBuilder.RenameColumn(
                name: "CertificateID",
                table: "DeviceFactoryData",
                newName: "CertificateId");

            migrationBuilder.RenameColumn(
                name: "CcTargetedversion",
                table: "DeviceFactoryData",
                newName: "CcTargetedVersion");

            migrationBuilder.RenameColumn(
                name: "CcDeviceversion",
                table: "DeviceFactoryData",
                newName: "CcDeviceVersion");

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "DeviceFactoryData",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
