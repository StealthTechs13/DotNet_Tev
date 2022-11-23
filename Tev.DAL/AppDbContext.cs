using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Tev.DAL.Entities;

namespace Tev.DAL
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<UserDevicePermission> UserDevicePermissions { get; set; }
        public DbSet<DeviceReplacement> DeviceReplacements { get; set; }
        public DbSet<Technician> Technicians { get; set; }
        public DbSet<EscalationMatrix> EscalationMatrices { get; set; }
        public DbSet<EmergencyCallHistory> EmergencyCallHistories { get; set; }
        public DbSet<ZohoSubscriptionHistory> ZohoSubscriptionHistories { get; set; }
        public DbSet<FeatureSubscriptionAssociation>  FeatureSubscriptionAssociations { get; set; }
        public DbSet<PaymentHistory> PaymentHistories { get; set; }
        public DbSet<PayementInvoiceAssociation> PayementInvoiceAssociations { get; set; }
        public DbSet<InvoiceSubscriptionAssociation> InvoiceSubscriptionAssociations { get; set; }
        public DbSet<InvoiceHistory> InvoiceHistories { get; set; }
        public DbSet<InvoiceHistoryItem> InvoiceHistoryItems { get; set; }
        public DbSet<InvoiceHistoryPayment> InvoiceHistoryPayments { get; set; }
        public DbSet<InvoiceHistorySubscription> InvoiceHistorySubscriptions { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<LiveStreaming> LiveStreamingRecords { get; set; }
        public DbSet<WSDTest> WSDTestRecords { get; set; }
        public DbSet<DeviceDetachedHistory> DeviceDetachedHistories { get; set; }
        public DbSet<QRAuthCode> QRAuthCodes { get; set; }
        public DbSet<PromoSubscriptionDevice> PromoSubscriptionDevices { get; set; }
        public DbSet<SRTRoutes> SRTRoutes { get; set; }
        public DbSet<DeviceFactoryData> DeviceFactoryData { get; set; }
        public DbSet<SrtSessionDetail> SrtSessionDetails { get; set; }
        public DbSet<SDCardHistory> SDCardHistory { get; set; }

        public DbSet<DeviceStreamingTypeManagement> DeviceStreamingTypeManagement { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if(modelBuilder != null)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.Entity<TechnicianDevices>().HasOne(z => z.Technician)
                    .WithMany(z => z.TechnicianDevices).HasForeignKey(z => z.TechnicianId).IsRequired(true);

                modelBuilder.Entity<FeatureSubscriptionAssociation>()
                    .HasOne(z => z.ZohoSubscriptionHistory)
                    .WithMany(z => z.Features)
                    .HasForeignKey(z => z.ZohoSubscriptionHistoryFK);

                modelBuilder.Entity<PayementInvoiceAssociation>()
                    .HasOne(z => z.PaymentHistory)
                    .WithMany(z => z.PayementInvoiceAssociations)
                    .HasForeignKey(z => z.PaymentHistoryFK);

                modelBuilder.Entity<InvoiceSubscriptionAssociation>()
                    .HasOne(z => z.PayementInvoiceAssociation)
                    .WithMany(z => z.InvoiceSubscriptionAssociations)
                    .HasForeignKey(z => z.PayementInvoiceAssociationFK);

                modelBuilder.Entity<InvoiceHistoryItem>()
                    .HasOne(z => z.InvoiceHistory)
                    .WithMany(z => z.InvoiceItems)
                    .HasForeignKey(z => z.InvoiceHistoryFK);

                modelBuilder.Entity<InvoiceHistoryPayment>()
                   .HasOne(z => z.InvoiceHistory)
                   .WithMany(z => z.Payments)
                   .HasForeignKey(z => z.InvoiceHistoryFK);

                modelBuilder.Entity<InvoiceHistorySubscription>()
                   .HasOne(z => z.InvoiceHistory)
                   .WithMany(z => z.InvoiceHistorySubscriptionAssociations)
                   .HasForeignKey(z => z.InvoiceHistoryFK);
            }
           
        }
    }
}
