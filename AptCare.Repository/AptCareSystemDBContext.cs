using AptCare.Repository.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository
{
    public class AptCareSystemDBContext : DbContext
    {
        public AptCareSystemDBContext(DbContextOptions<AptCareSystemDBContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<AccountToken> AccountTokens { get; set; }
        public DbSet<AccountOTPHistory> AccountOTPHistories { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Floor> Floors { get; set; }
        public DbSet<Apartment> Apartments { get; set; }
        public DbSet<CommonArea> CommonAreas { get; set; }
        public DbSet<UserApartment> UserApartments { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<TechnicianTechnique> TechnicianTechniques { get; set; }
        public DbSet<Technique> Techniques { get; set; }
        public DbSet<WorkSlot> WorkSlots { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình chỉ mục duy nhất cho Phone và Email trong bảng User
            modelBuilder.Entity<User>()
                .HasIndex(u => u.PhoneNumber)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Cấu hình mối quan hệ 1-1 giữa User và Account
            modelBuilder.Entity<User>()
                .HasOne(u => u.Account)
                .WithOne(a => a.User)
                .HasForeignKey<Account>(a => a.AccountId); // AccountId là FK tới User

            // Cấu hình mối quan hệ 1-n giữa Account và AccountToken
            modelBuilder.Entity<AccountToken>()
                .HasOne(at => at.Account)
                .WithMany(a => a.AccountTokens)
                .HasForeignKey(at => at.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình mối quan hệ 1-n giữa Account và AccountOTPHistory
            modelBuilder.Entity<AccountOTPHistory>()
                .HasOne(otp => otp.Account)
                .WithMany(a => a.AccountOTPHistories)
                .HasForeignKey(otp => otp.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình mối quan hệ 1-n giữa Account và Notification (ReceiverId)
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Receiver)
                .WithMany(a => a.Notifications)
                .HasForeignKey(n => n.ReceiverId)
                .OnDelete(DeleteBehavior.Cascade);
            // Cấu hình mối quan hệ 1-n giữa Floor và Apartment
            modelBuilder.Entity<Apartment>()
                .HasOne(a => a.Floor)
                .WithMany(f => f.Apartments)
                .HasForeignKey(a => a.FloorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình mối quan hệ 1-n giữa Floor và CommonArea
            modelBuilder.Entity<CommonArea>()
                .HasOne(ca => ca.Floor)
                .WithMany(f => f.CommonAreas)
                .HasForeignKey(ca => ca.FloorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình mối quan hệ n-n giữa User và Apartment thông qua UserApartment
            modelBuilder.Entity<UserApartment>()
                .HasOne(ua => ua.User)
                .WithMany(u => u.UserApartments)
                .HasForeignKey(ua => ua.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserApartment>()
                .HasOne(ua => ua.Apartment)
                .WithMany(a => a.UserApartments)
                .HasForeignKey(ua => ua.ApartmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình mối quan hệ 1-n giữa User và Report
            modelBuilder.Entity<Report>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reports)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình mối quan hệ 1-n giữa CommonArea và Report
            modelBuilder.Entity<Report>()
                .HasOne(r => r.CommonArea)
                .WithMany(ca => ca.Reports)
                .HasForeignKey(r => r.CommonAreaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TechnicianTechnique>()
                .HasKey(tt => new { tt.TechnicianId, tt.TechniqueId });
            modelBuilder.Entity<TechnicianTechnique>()
                .HasOne(tt => tt.Technician)
                .WithMany(u => u.TechnicianTechniques)
                .HasForeignKey(tt => tt.TechnicianId);
            modelBuilder.Entity<TechnicianTechnique>()
                .HasOne(tt => tt.Technique)
                .WithMany(t => t.TechnicianTechniques)
                .HasForeignKey(tt => tt.TechniqueId);

        }
    }
}
