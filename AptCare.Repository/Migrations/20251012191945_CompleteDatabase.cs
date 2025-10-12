using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AptCare.Repository.Migrations
{
    /// <inheritdoc />
    public partial class CompleteDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_conversationParticipants_Users_ParticipantId",
                table: "conversationParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_conversationParticipants_conversations_ConversationId",
                table: "conversationParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_messages_Users_SenderId",
                table: "messages");

            migrationBuilder.DropForeignKey(
                name: "FK_messages_conversations_ConversationId",
                table: "messages");

            migrationBuilder.DropForeignKey(
                name: "FK_messages_messages_ReplyMessageId",
                table: "messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_messages_MessageId",
                table: "Notifications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_messages",
                table: "messages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_conversations",
                table: "conversations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_conversationParticipants",
                table: "conversationParticipants");

            migrationBuilder.RenameTable(
                name: "messages",
                newName: "Messages");

            migrationBuilder.RenameTable(
                name: "conversations",
                newName: "Conversations");

            migrationBuilder.RenameTable(
                name: "conversationParticipants",
                newName: "ConversationParticipants");

            migrationBuilder.RenameIndex(
                name: "IX_messages_SenderId",
                table: "Messages",
                newName: "IX_Messages_SenderId");

            migrationBuilder.RenameIndex(
                name: "IX_messages_ReplyMessageId",
                table: "Messages",
                newName: "IX_Messages_ReplyMessageId");

            migrationBuilder.RenameIndex(
                name: "IX_messages_ConversationId",
                table: "Messages",
                newName: "IX_Messages_ConversationId");

            migrationBuilder.RenameIndex(
                name: "IX_conversationParticipants_ConversationId",
                table: "ConversationParticipants",
                newName: "IX_ConversationParticipants_ConversationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Messages",
                table: "Messages",
                column: "MessageId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Conversations",
                table: "Conversations",
                column: "ConversationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ConversationParticipants",
                table: "ConversationParticipants",
                columns: new[] { "ParticipantId", "ConversationId" });

            migrationBuilder.CreateTable(
                name: "Accessories",
                columns: table => new
                {
                    AccessoryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accessories", x => x.AccessoryId);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceRequests",
                columns: table => new
                {
                    MaintenanceRequestId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CommonAreaId = table.Column<int>(type: "integer", nullable: false),
                    Object = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Frequency = table.Column<int>(type: "integer", nullable: false),
                    NextDay = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceRequests", x => x.MaintenanceRequestId);
                    table.ForeignKey(
                        name: "FK_MaintenanceRequests_CommonAreas_CommonAreaId",
                        column: x => x.CommonAreaId,
                        principalTable: "CommonAreas",
                        principalColumn: "CommonAreaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Medias",
                columns: table => new
                {
                    MediaId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntityId = table.Column<int>(type: "integer", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medias", x => x.MediaId);
                });

            migrationBuilder.CreateTable(
                name: "RepairReports",
                columns: table => new
                {
                    RepairReportId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppointmentId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairReports", x => x.RepairReportId);
                    table.ForeignKey(
                        name: "FK_RepairReports_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceTrackingHistories",
                columns: table => new
                {
                    MaintenanceTrackingHistoryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaintenanceRequestId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Field = table.Column<string>(type: "text", nullable: false),
                    OldValue = table.Column<string>(type: "text", nullable: false),
                    NewValue = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceTrackingHistories", x => x.MaintenanceTrackingHistoryId);
                    table.ForeignKey(
                        name: "FK_MaintenanceTrackingHistories_MaintenanceRequests_Maintenanc~",
                        column: x => x.MaintenanceRequestId,
                        principalTable: "MaintenanceRequests",
                        principalColumn: "MaintenanceRequestId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaintenanceTrackingHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RepairRequests",
                columns: table => new
                {
                    RepairRequestId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ApartmentId = table.Column<int>(type: "integer", nullable: true),
                    CommonAreaId = table.Column<int>(type: "integer", nullable: true),
                    ParentRequestId = table.Column<int>(type: "integer", nullable: true),
                    TechniqueId = table.Column<int>(type: "integer", nullable: true),
                    MaintenanceRequestId = table.Column<int>(type: "integer", nullable: true),
                    IsEmergency = table.Column<bool>(type: "boolean", nullable: false),
                    Object = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcceptanceTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairRequests", x => x.RepairRequestId);
                    table.ForeignKey(
                        name: "FK_RepairRequests_Apartments_ApartmentId",
                        column: x => x.ApartmentId,
                        principalTable: "Apartments",
                        principalColumn: "ApartmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairRequests_CommonAreas_CommonAreaId",
                        column: x => x.CommonAreaId,
                        principalTable: "CommonAreas",
                        principalColumn: "CommonAreaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairRequests_MaintenanceRequests_MaintenanceRequestId",
                        column: x => x.MaintenanceRequestId,
                        principalTable: "MaintenanceRequests",
                        principalColumn: "MaintenanceRequestId");
                    table.ForeignKey(
                        name: "FK_RepairRequests_RepairRequests_ParentRequestId",
                        column: x => x.ParentRequestId,
                        principalTable: "RepairRequests",
                        principalColumn: "RepairRequestId");
                    table.ForeignKey(
                        name: "FK_RepairRequests_Techniques_TechniqueId",
                        column: x => x.TechniqueId,
                        principalTable: "Techniques",
                        principalColumn: "TechniqueId");
                    table.ForeignKey(
                        name: "FK_RepairRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    AppointmentId = table.Column<int>(type: "integer", nullable: false),
                    RepairRequestId = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.AppointmentId);
                    table.ForeignKey(
                        name: "FK_Appointments_RepairReports_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "RepairReports",
                        principalColumn: "RepairReportId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Appointments_RepairRequests_RepairRequestId",
                        column: x => x.RepairRequestId,
                        principalTable: "RepairRequests",
                        principalColumn: "RepairRequestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Contracts",
                columns: table => new
                {
                    ContractId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RepairRequestId = table.Column<int>(type: "integer", nullable: true),
                    ContractorName = table.Column<string>(type: "text", nullable: false),
                    ContractCode = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contracts", x => x.ContractId);
                    table.ForeignKey(
                        name: "FK_Contracts_RepairRequests_RepairRequestId",
                        column: x => x.RepairRequestId,
                        principalTable: "RepairRequests",
                        principalColumn: "RepairRequestId");
                });

            migrationBuilder.CreateTable(
                name: "Feedbacks",
                columns: table => new
                {
                    FeedbackId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RepairRequestId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ParentFeedbackId = table.Column<int>(type: "integer", nullable: true),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedbacks", x => x.FeedbackId);
                    table.ForeignKey(
                        name: "FK_Feedbacks_Feedbacks_ParentFeedbackId",
                        column: x => x.ParentFeedbackId,
                        principalTable: "Feedbacks",
                        principalColumn: "FeedbackId");
                    table.ForeignKey(
                        name: "FK_Feedbacks_RepairRequests_RepairRequestId",
                        column: x => x.RepairRequestId,
                        principalTable: "RepairRequests",
                        principalColumn: "RepairRequestId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Feedbacks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    InvoiceId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RepairRequestId = table.Column<int>(type: "integer", nullable: false),
                    IsChargeable = table.Column<bool>(type: "boolean", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.InvoiceId);
                    table.ForeignKey(
                        name: "FK_Invoices_RepairRequests_RepairRequestId",
                        column: x => x.RepairRequestId,
                        principalTable: "RepairRequests",
                        principalColumn: "RepairRequestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestTrackings",
                columns: table => new
                {
                    RequestTrackingId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RepairRequestId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestTrackings", x => x.RequestTrackingId);
                    table.ForeignKey(
                        name: "FK_RequestTrackings_RepairRequests_RepairRequestId",
                        column: x => x.RepairRequestId,
                        principalTable: "RepairRequests",
                        principalColumn: "RepairRequestId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestTrackings_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "AppointmentAssigns",
                columns: table => new
                {
                    AppointmentAssignId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppointmentId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentAssigns", x => x.AppointmentAssignId);
                    table.ForeignKey(
                        name: "FK_AppointmentAssigns_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "AppointmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppointmentAssigns_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InspectionReports",
                columns: table => new
                {
                    InspectionReportId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppointmentId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    FaultOwner = table.Column<int>(type: "integer", nullable: false),
                    SolutionType = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Solution = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionReports", x => x.InspectionReportId);
                    table.ForeignKey(
                        name: "FK_InspectionReports_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "AppointmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InspectionReports_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrders",
                columns: table => new
                {
                    WorkOrderId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppointmentId = table.Column<int>(type: "integer", nullable: false),
                    TechnicianId = table.Column<int>(type: "integer", nullable: false),
                    EstimatedStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EstimatedEndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActualEndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrders", x => x.WorkOrderId);
                    table.ForeignKey(
                        name: "FK_WorkOrders_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "AppointmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkOrders_Users_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceItems",
                columns: table => new
                {
                    InvoiceItemId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvoiceId = table.Column<int>(type: "integer", nullable: false),
                    AccessoryId = table.Column<int>(type: "integer", nullable: false),
                    ItemName = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceItems", x => x.InvoiceItemId);
                    table.ForeignKey(
                        name: "FK_InvoiceItems_Accessories_AccessoryId",
                        column: x => x.AccessoryId,
                        principalTable: "Accessories",
                        principalColumn: "AccessoryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvoiceItems_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "InvoiceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    TransactionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    InvoiceId = table.Column<int>(type: "integer", nullable: false),
                    TransactionType = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_Transactions_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "InvoiceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Transactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportApprovals",
                columns: table => new
                {
                    ReportApprovalId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InspectionReportId = table.Column<int>(type: "integer", nullable: false),
                    RepairReportId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportApprovals", x => x.ReportApprovalId);
                    table.ForeignKey(
                        name: "FK_ReportApprovals_InspectionReports_InspectionReportId",
                        column: x => x.InspectionReportId,
                        principalTable: "InspectionReports",
                        principalColumn: "InspectionReportId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportApprovals_RepairReports_RepairReportId",
                        column: x => x.RepairReportId,
                        principalTable: "RepairReports",
                        principalColumn: "RepairReportId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportApprovals_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentAssigns_AppointmentId",
                table: "AppointmentAssigns",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentAssigns_UserId",
                table: "AppointmentAssigns",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_RepairRequestId",
                table: "Appointments",
                column: "RepairRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_RepairRequestId",
                table: "Contracts",
                column: "RepairRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_ParentFeedbackId",
                table: "Feedbacks",
                column: "ParentFeedbackId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_RepairRequestId",
                table: "Feedbacks",
                column: "RepairRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_UserId",
                table: "Feedbacks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionReports_AppointmentId",
                table: "InspectionReports",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionReports_UserId",
                table: "InspectionReports",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_AccessoryId",
                table: "InvoiceItems",
                column: "AccessoryId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_InvoiceId",
                table: "InvoiceItems",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_RepairRequestId",
                table: "Invoices",
                column: "RepairRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_CommonAreaId",
                table: "MaintenanceRequests",
                column: "CommonAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTrackingHistories_MaintenanceRequestId",
                table: "MaintenanceTrackingHistories",
                column: "MaintenanceRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTrackingHistories_UserId",
                table: "MaintenanceTrackingHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Medias_EntityType_EntityId",
                table: "Medias",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_RepairReports_UserId",
                table: "RepairReports",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairRequests_ApartmentId",
                table: "RepairRequests",
                column: "ApartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairRequests_CommonAreaId",
                table: "RepairRequests",
                column: "CommonAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairRequests_MaintenanceRequestId",
                table: "RepairRequests",
                column: "MaintenanceRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairRequests_ParentRequestId",
                table: "RepairRequests",
                column: "ParentRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairRequests_TechniqueId",
                table: "RepairRequests",
                column: "TechniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairRequests_UserId",
                table: "RepairRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportApprovals_InspectionReportId",
                table: "ReportApprovals",
                column: "InspectionReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportApprovals_RepairReportId",
                table: "ReportApprovals",
                column: "RepairReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportApprovals_UserId",
                table: "ReportApprovals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTrackings_RepairRequestId",
                table: "RequestTrackings",
                column: "RepairRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTrackings_UpdatedBy",
                table: "RequestTrackings",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_InvoiceId",
                table: "Transactions",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId",
                table: "Transactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_AppointmentId",
                table: "WorkOrders",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_TechnicianId",
                table: "WorkOrders",
                column: "TechnicianId");

            migrationBuilder.AddForeignKey(
                name: "FK_ConversationParticipants_Conversations_ConversationId",
                table: "ConversationParticipants",
                column: "ConversationId",
                principalTable: "Conversations",
                principalColumn: "ConversationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ConversationParticipants_Users_ParticipantId",
                table: "ConversationParticipants",
                column: "ParticipantId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Conversations_ConversationId",
                table: "Messages",
                column: "ConversationId",
                principalTable: "Conversations",
                principalColumn: "ConversationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Messages_ReplyMessageId",
                table: "Messages",
                column: "ReplyMessageId",
                principalTable: "Messages",
                principalColumn: "MessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Users_SenderId",
                table: "Messages",
                column: "SenderId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Messages_MessageId",
                table: "Notifications",
                column: "MessageId",
                principalTable: "Messages",
                principalColumn: "MessageId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConversationParticipants_Conversations_ConversationId",
                table: "ConversationParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_ConversationParticipants_Users_ParticipantId",
                table: "ConversationParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Conversations_ConversationId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Messages_ReplyMessageId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Users_SenderId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Messages_MessageId",
                table: "Notifications");

            migrationBuilder.DropTable(
                name: "AppointmentAssigns");

            migrationBuilder.DropTable(
                name: "Contracts");

            migrationBuilder.DropTable(
                name: "Feedbacks");

            migrationBuilder.DropTable(
                name: "InvoiceItems");

            migrationBuilder.DropTable(
                name: "MaintenanceTrackingHistories");

            migrationBuilder.DropTable(
                name: "Medias");

            migrationBuilder.DropTable(
                name: "ReportApprovals");

            migrationBuilder.DropTable(
                name: "RequestTrackings");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "WorkOrders");

            migrationBuilder.DropTable(
                name: "Accessories");

            migrationBuilder.DropTable(
                name: "InspectionReports");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "RepairReports");

            migrationBuilder.DropTable(
                name: "RepairRequests");

            migrationBuilder.DropTable(
                name: "MaintenanceRequests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Messages",
                table: "Messages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Conversations",
                table: "Conversations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ConversationParticipants",
                table: "ConversationParticipants");

            migrationBuilder.RenameTable(
                name: "Messages",
                newName: "messages");

            migrationBuilder.RenameTable(
                name: "Conversations",
                newName: "conversations");

            migrationBuilder.RenameTable(
                name: "ConversationParticipants",
                newName: "conversationParticipants");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_SenderId",
                table: "messages",
                newName: "IX_messages_SenderId");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_ReplyMessageId",
                table: "messages",
                newName: "IX_messages_ReplyMessageId");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_ConversationId",
                table: "messages",
                newName: "IX_messages_ConversationId");

            migrationBuilder.RenameIndex(
                name: "IX_ConversationParticipants_ConversationId",
                table: "conversationParticipants",
                newName: "IX_conversationParticipants_ConversationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_messages",
                table: "messages",
                column: "MessageId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_conversations",
                table: "conversations",
                column: "ConversationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_conversationParticipants",
                table: "conversationParticipants",
                columns: new[] { "ParticipantId", "ConversationId" });

            migrationBuilder.AddForeignKey(
                name: "FK_conversationParticipants_Users_ParticipantId",
                table: "conversationParticipants",
                column: "ParticipantId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_conversationParticipants_conversations_ConversationId",
                table: "conversationParticipants",
                column: "ConversationId",
                principalTable: "conversations",
                principalColumn: "ConversationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_messages_Users_SenderId",
                table: "messages",
                column: "SenderId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_messages_conversations_ConversationId",
                table: "messages",
                column: "ConversationId",
                principalTable: "conversations",
                principalColumn: "ConversationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_messages_messages_ReplyMessageId",
                table: "messages",
                column: "ReplyMessageId",
                principalTable: "messages",
                principalColumn: "MessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_messages_MessageId",
                table: "Notifications",
                column: "MessageId",
                principalTable: "messages",
                principalColumn: "MessageId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
