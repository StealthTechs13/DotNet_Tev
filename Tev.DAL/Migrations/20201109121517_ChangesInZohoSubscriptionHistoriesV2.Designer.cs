﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Tev.DAL;

namespace Tev.DAL.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20201109121517_ChangesInZohoSubscriptionHistoriesV2")]
    partial class ChangesInZohoSubscriptionHistoriesV2
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Tev.DAL.Entities.DeviceReplacement", b =>
                {
                    b.Property<string>("DeviceReplacementId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Address")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Comments")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("CreatedDate")
                        .HasColumnType("bigint");

                    b.Property<string>("DeviceId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Email")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("ExpectedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("ModifiedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long?>("ModifiedDate")
                        .HasColumnType("bigint");

                    b.Property<string>("OrgId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("RaiseBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("ReplaceStatus")
                        .HasColumnType("int");

                    b.HasKey("DeviceReplacementId");

                    b.ToTable("DeviceReplacements");
                });

            modelBuilder.Entity("Tev.DAL.Entities.EmergencyCallHistory", b =>
                {
                    b.Property<string>("EmergencyCallHistoryId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("CallingPurpose")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("CreatedDate")
                        .HasColumnType("bigint");

                    b.Property<string>("DeviceId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ModifiedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long?>("ModifiedDate")
                        .HasColumnType("bigint");

                    b.Property<string>("Number")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("Time")
                        .HasColumnType("datetime2");

                    b.HasKey("EmergencyCallHistoryId");

                    b.ToTable("EmergencyCallHistories");
                });

            modelBuilder.Entity("Tev.DAL.Entities.EscalationMatrix", b =>
                {
                    b.Property<string>("EscalationMatrixId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<decimal>("AttentionTime")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("CreatedDate")
                        .HasColumnType("bigint");

                    b.Property<string>("DeviceId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("EscalationLevel")
                        .HasColumnType("int");

                    b.Property<string>("ModifiedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long?>("ModifiedDate")
                        .HasColumnType("bigint");

                    b.Property<int>("OrganizationId")
                        .HasColumnType("int");

                    b.Property<string>("ReceiverDescription")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ReceiverName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ReceiverPhone")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SenderPhone")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("SmokeStatus")
                        .HasColumnType("int");

                    b.Property<int>("SmokeValue")
                        .HasColumnType("int");

                    b.HasKey("EscalationMatrixId");

                    b.ToTable("EscalationMatrices");
                });

            modelBuilder.Entity("Tev.DAL.Entities.FeatureSubscriptionAssociation", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("CreatedDate")
                        .HasColumnType("bigint");

                    b.Property<string>("ModifiedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long?>("ModifiedDate")
                        .HasColumnType("bigint");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<double>("Price")
                        .HasColumnType("float");

                    b.Property<int>("SubscriptionId")
                        .HasColumnType("int");

                    b.Property<int>("ZohoSubscriptionHistoryFK")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("ZohoSubscriptionHistoryFK");

                    b.ToTable("FeatureSubscriptionAssociations");
                });

            modelBuilder.Entity("Tev.DAL.Entities.Location", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("CreatedDate")
                        .HasColumnType("bigint");

                    b.Property<string>("ModifiedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long?>("ModifiedDate")
                        .HasColumnType("bigint");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("OrgId")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Locations");
                });

            modelBuilder.Entity("Tev.DAL.Entities.Technician", b =>
                {
                    b.Property<string>("TechnicianId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("CreatedDate")
                        .HasColumnType("bigint");

                    b.Property<string>("Email")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Latitude")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Longitude")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ModifiedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long?>("ModifiedDate")
                        .HasColumnType("bigint");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Phone")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("TechnicianType")
                        .HasColumnType("int");

                    b.HasKey("TechnicianId");

                    b.ToTable("Technicians");
                });

            modelBuilder.Entity("Tev.DAL.Entities.TechnicianDevices", b =>
                {
                    b.Property<string>("TechnicianDeviceId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("CreatedDate")
                        .HasColumnType("bigint");

                    b.Property<int>("DeviceType")
                        .HasColumnType("int");

                    b.Property<string>("ModifiedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long?>("ModifiedDate")
                        .HasColumnType("bigint");

                    b.Property<string>("TechnicianId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("TechnicianDeviceId");

                    b.HasIndex("TechnicianId");

                    b.ToTable("TechnicianDevices");
                });

            modelBuilder.Entity("Tev.DAL.Entities.UserDevicePermission", b =>
                {
                    b.Property<string>("UserDevicePermissionId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("CreatedDate")
                        .HasColumnType("bigint");

                    b.Property<string>("DeviceId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("DevicePermission")
                        .HasColumnType("int");

                    b.Property<int>("DeviceType")
                        .HasColumnType("int");

                    b.Property<string>("ModifiedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long?>("ModifiedDate")
                        .HasColumnType("bigint");

                    b.Property<string>("UserEmail")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("UserDevicePermissionId");

                    b.ToTable("UserDevicePermissions");
                });

            modelBuilder.Entity("Tev.DAL.Entities.ZohoSubscriptionHistory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<double>("Amount")
                        .HasColumnType("float");

                    b.Property<double>("CGSTAmount")
                        .HasColumnType("float");

                    b.Property<string>("CGSTName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CompanyName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("CreatedDate")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("CreatedTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("DeviceId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("DeviceName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Email")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("EventType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ModifiedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long?>("ModifiedDate")
                        .HasColumnType("bigint");

                    b.Property<string>("PlanCode")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PlanName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<double>("PlanPrice")
                        .HasColumnType("float");

                    b.Property<string>("ProductName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<double>("SGSTAmount")
                        .HasColumnType("float");

                    b.Property<string>("SGSTName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Status")
                        .HasColumnType("nvarchar(max)");

                    b.Property<double>("SubTotal")
                        .HasColumnType("float");

                    b.Property<string>("SubscriptionId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("TaxPercentage")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("ZohoSubscriptionHistories");
                });

            modelBuilder.Entity("Tev.DAL.Entities.FeatureSubscriptionAssociation", b =>
                {
                    b.HasOne("Tev.DAL.Entities.ZohoSubscriptionHistory", "ZohoSubscriptionHistory")
                        .WithMany("Features")
                        .HasForeignKey("ZohoSubscriptionHistoryFK")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Tev.DAL.Entities.TechnicianDevices", b =>
                {
                    b.HasOne("Tev.DAL.Entities.Technician", "Technician")
                        .WithMany("TechnicianDevices")
                        .HasForeignKey("TechnicianId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
