using AptCare.Repository.Entities;
using Microsoft.EntityFrameworkCore;
using System;

namespace AptCare.Repository
{
    public class AptCareSystemDBContext : DbContext
    {
        public AptCareSystemDBContext(DbContextOptions<AptCareSystemDBContext> options) : base(options) { }

        // ========================= DbSet =========================
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
        public DbSet<WorkSlotStatusTracking> WorkSlotStatusTrackings { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<ConversationParticipant> ConversationParticipants { get; set; }

        public DbSet<Accessory> Accessories { get; set; }
        public DbSet<RepairRequest> RepairRequests { get; set; }
        public DbSet<RepairReport> RepairReports { get; set; }
        public DbSet<InspectionReport> InspectionReports { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<AppointmentAssign> AppointmentAssigns { get; set; }
        public DbSet<ReportApproval> ReportApprovals { get; set; }
        public DbSet<RequestTracking> RequestTrackings { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<WorkOrder> WorkOrders { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<MaintenanceRequest> MaintenanceRequests { get; set; }
        public DbSet<MaintenanceTrackingHistory> MaintenanceTrackingHistories { get; set; }
        public DbSet<Media> Medias { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ========================= User & Account =========================
            // User - Account (1 - 1)
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.PhoneNumber).IsUnique();
                entity.HasIndex(u => u.CitizenshipIdentity).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();

                entity.HasOne(u => u.Account)
                      .WithOne(a => a.User)
                      .HasForeignKey<Account>(a => a.AccountId);
            });

            // Account - AccountToken (1 - n)
            modelBuilder.Entity<AccountToken>()
                .HasOne(at => at.Account)
                .WithMany(a => a.AccountTokens)
                .HasForeignKey(at => at.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // Account - AccountOTPHistory (1 - n)
            modelBuilder.Entity<AccountOTPHistory>()
                .HasOne(otp => otp.Account)
                .WithMany(a => a.AccountOTPHistories)
                .HasForeignKey(otp => otp.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // ========================= Notification =========================
            // User(Account) - Notification (1 - n)
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Receiver)
                .WithMany(a => a.Notifications)
                .HasForeignKey(n => n.ReceiverId)
                .OnDelete(DeleteBehavior.Cascade);

            // Message - Notification (1 - 1)
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Message)
                .WithOne(m => m.Notification)
                .HasForeignKey<Notification>(n => n.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            // ========================= Building =========================
            // Floor - Apartment (1 - n)
            modelBuilder.Entity<Apartment>()
                .HasOne(a => a.Floor)
                .WithMany(f => f.Apartments)
                .HasForeignKey(a => a.FloorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Floor - CommonArea (1 - n)
            modelBuilder.Entity<CommonArea>()
                .HasOne(ca => ca.Floor)
                .WithMany(f => f.CommonAreas)
                .HasForeignKey(ca => ca.FloorId)
                .OnDelete(DeleteBehavior.Restrict);

            // User - Apartment (n - n)
            modelBuilder.Entity<UserApartment>(entity =>
            {
                entity.HasKey(ua => new { ua.UserId, ua.ApartmentId });
                entity.HasOne(ua => ua.User)
                      .WithMany(u => u.UserApartments)
                      .HasForeignKey(ua => ua.UserId);
                entity.HasOne(ua => ua.Apartment)
                      .WithMany(a => a.UserApartments)
                      .HasForeignKey(ua => ua.ApartmentId);
            });

            // ========================= Report =========================
            // User - Report (1 - n)
            // CommonArea - Report (1 - n)
            modelBuilder.Entity<Report>(entity =>
            {
                entity.HasOne(r => r.User)
                      .WithMany(u => u.Reports)
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.CommonArea)
                      .WithMany(ca => ca.Reports)
                      .HasForeignKey(r => r.CommonAreaId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ========================= Technician & Technique =========================
            // Technician - Technique (n - n)
            modelBuilder.Entity<TechnicianTechnique>(entity =>
            {
                entity.HasKey(tt => new { tt.TechnicianId, tt.TechniqueId });
                entity.HasOne(tt => tt.Technician)
                      .WithMany(u => u.TechnicianTechniques)
                      .HasForeignKey(tt => tt.TechnicianId);
                entity.HasOne(tt => tt.Technique)
                      .WithMany(t => t.TechnicianTechniques)
                      .HasForeignKey(tt => tt.TechniqueId);
            });

            // ========================= WorkSlot =========================
            // Technician - WorkSlot (1 - n)
            modelBuilder.Entity<WorkSlot>()
                .HasOne(ws => ws.Technician)
                .WithMany(u => u.WorkSlots)
                .HasForeignKey(ws => ws.TechnicianId)
                .OnDelete(DeleteBehavior.Restrict);

            // WorkSlot - WorkSlotStatusTracking (1 - n)
            modelBuilder.Entity<WorkSlotStatusTracking>()
                .HasOne(wst => wst.WorkSlot)
                .WithMany(ws => ws.WorkSlotStatusTrackings)
                .HasForeignKey(wst => wst.WorkSlotId)
                .OnDelete(DeleteBehavior.Cascade);

            // ========================= Chat =========================
            // Conversation - ConversationParticipant (1 - n)
            // Conversation - Message (1 - n)
            modelBuilder.Entity<ConversationParticipant>(entity =>
            {
                entity.HasKey(cp => new { cp.ParticipantId, cp.ConversationId });
                entity.HasOne(cp => cp.Participant)
                      .WithMany(u => u.ConversationParticipants)
                      .HasForeignKey(cp => cp.ParticipantId);
                entity.HasOne(cp => cp.Conversation)
                      .WithMany(c => c.ConversationParticipants)
                      .HasForeignKey(cp => cp.ConversationId);
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasOne(m => m.Sender)
                      .WithMany(u => u.Messages)
                      .HasForeignKey(m => m.SenderId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(m => m.Conversation)
                      .WithMany(c => c.Messages)
                      .HasForeignKey(m => m.ConversationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ========================= RepairRequest Flow =========================
            // User - RepairRequest (1 - n)
            // Apartment - RepairRequest (1 - n)
            // CommonArea - RepairRequest (1 - n)
            // Technician - RepairRequest (1 - n)
            modelBuilder.Entity<RepairRequest>(entity =>
            {
                entity.HasOne(rr => rr.User)
                      .WithMany(u => u.RepairRequests)
                      .HasForeignKey(rr => rr.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(rr => rr.Apartment)
                      .WithMany(a => a.RepairRequests)
                      .HasForeignKey(rr => rr.ApartmentId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(rr => rr.CommonArea)
                      .WithMany(ca => ca.RepairRequests)
                      .HasForeignKey(rr => rr.CommonAreaId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(rr => rr.MaintenanceRequest)
                      .WithMany(mr => mr.RepairRequests)
                      .HasForeignKey(rr => rr.MaintenanceRequestId);
                entity.HasOne(rr => rr.Technique)
                      .WithMany(mr => mr.RepairRequests)
                      .HasForeignKey(rr => rr.TechniqueId);
            });

            // RepairRequest - Appointment (1 - n)
            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.HasOne(a => a.RepairRequest)
                      .WithMany(rr => rr.Appointments)
                      .HasForeignKey(a => a.RepairRequestId);
            });

            // Appointment - AppointmentAssign (1 - n)
            // Appointment - InspectionReport (1 - n)
            modelBuilder.Entity<AppointmentAssign>(entity =>
            {
                entity.HasOne(aa => aa.Appointment)
                      .WithMany(a => a.AppointmentAssigns)
                      .HasForeignKey(aa => aa.AppointmentId);
            });

            modelBuilder.Entity<InspectionReport>(entity =>
            {
                entity.HasOne(ir => ir.Appointment)
                      .WithMany(a => a.InspectionReports)
                      .HasForeignKey(ir => ir.AppointmentId);
            });

            // Appointment - RepairReport (1 - 1)
            // RepairRequest - RepairReport (1 - n)
            modelBuilder.Entity<RepairReport>(entity =>
            {
                entity.HasOne(rr => rr.Appointment)
                      .WithOne(r => r.RepairReport)
                      .HasForeignKey<Appointment>(a => a.AppointmentId);
            });

            // RepairReport - ReportApproval (1 - n)
            modelBuilder.Entity<ReportApproval>(entity =>
            {
                entity.HasOne(ra => ra.RepairReport)
                      .WithMany(rr => rr.ReportApprovals)
                      .HasForeignKey(ra => ra.RepairReportId);
            });

            // RepairRequest - RequestTracking (1 - n)
            modelBuilder.Entity<RequestTracking>(entity =>
            {
                entity.HasOne(rt => rt.RepairRequest)
                      .WithMany(rr => rr.RequestTrackings)
                      .HasForeignKey(rt => rt.RepairRequestId);
            });

            // RepairRequest - Feedback (1 - n)
            modelBuilder.Entity<Feedback>(entity =>
            {
                entity.HasOne(f => f.RepairRequest)
                      .WithMany(rr => rr.Feedbacks)
                      .HasForeignKey(f => f.RepairRequestId);
            });

            // RepairRequest - Invoice (1 - n)
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasOne(i => i.RepairRequest)
                      .WithMany(rr => rr.Invoices)
                      .HasForeignKey(i => i.RepairRequestId);
            });

            // Invoice - InvoiceItem (1 - n)
            // Accessory - InvoiceItem (1 - n)
            modelBuilder.Entity<InvoiceItem>(entity =>
            {
                entity.HasOne(ii => ii.Invoice)
                      .WithMany(i => i.InvoiceItems)
                      .HasForeignKey(ii => ii.InvoiceId);
                entity.HasOne(ii => ii.Accessory)
                      .WithMany(a => a.InvoiceItems)
                      .HasForeignKey(ii => ii.AccessoryId);
            });

            // Invoice - Transaction (1 - n)
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasOne(t => t.Invoice)
                      .WithMany(i => i.Transactions)
                      .HasForeignKey(t => t.InvoiceId);
            });

            // WorkSlot - WorkOrder (1 - n)
            modelBuilder.Entity<WorkOrder>(entity =>
            {
                entity.HasOne(wo => wo.Appointment)
                      .WithMany(ws => ws.WorkOrders)
                      .HasForeignKey(wo => wo.AppointmentId);
            });

            // RepairRequest - Contract (1 - n)
            modelBuilder.Entity<Contract>(entity =>
            {
                entity.HasOne(c => c.RepairRequest)
                      .WithMany(rr => rr.Contracts)
                      .HasForeignKey(c => c.RepairRequestId);
            });

            // CommonArea - MaintenanceRequest (1 - n)
            // MaintenanceRequest - MaintenanceTrackingHistory (1 - n)
            modelBuilder.Entity<MaintenanceRequest>(entity =>
            {
                entity.HasOne(mr => mr.CommonArea)
                      .WithMany(ca => ca.MaintenanceRequests)
                      .HasForeignKey(mr => mr.CommonAreaId);
            });

            modelBuilder.Entity<MaintenanceTrackingHistory>(entity =>
            {
                entity.HasOne(mth => mth.MaintenanceRequest)
                      .WithMany(mr => mr.MaintenanceTrackingHistories)
                      .HasForeignKey(mth => mth.MaintenanceRequestId);
            });

            // Media generic (EntityType + EntityId)
            modelBuilder.Entity<Media>()
                        .HasIndex(m => new { m.EntityType, m.EntityId });
        }
    }
}
