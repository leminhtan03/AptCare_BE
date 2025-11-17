using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Services.Background;
using AptCare.Service.Services.Implements;
using AptCare.Service.Services.Implements.S3File;
using AptCare.Service.Services.Interfaces;
using AptCare.Service.Services.Interfaces.IS3File;
using AptCare.Service.Services.PayOSService;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Net.Mail;

namespace AptCare.Api.Extensions
{
    public static class ServiceExtension
    {
        public static IServiceCollection AddService(this IServiceCollection service)
        {

            service.AddScoped(typeof(IUnitOfWork<>), typeof(UnitOfWork<>));
            service.AddTransient<IUnitOfWork, UnitOfWork<AptCareSystemDBContext>>();
            service.AddTransient(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            service.AddTransient<IPasswordHasher<Account>, PasswordHasher<Account>>();
            service.AddTransient<IUserContext, UserContext>();
            service.AddTransient<IAccountService, AccountService>();
            service.AddTransient<IUserService, UserService>();
            service.AddTransient<ITokenService, TokenService>();
            service.AddTransient<IAccountService, AccountService>();
            service.AddTransient<IFloorService, FloorService>();
            service.AddTransient<IApartmentService, ApartmentService>();
            service.AddTransient<IUserService, UserService>();
            service.AddTransient<ICommonAreaService, CommonAreaService>();
            service.AddTransient<IWorkSlotService, WorkSlotService>();
            service.AddTransient<IMailSenderService, MailSenderService>();
            service.AddTransient<IConversationService, ConversationService>();
            service.AddTransient<IMessageService, MessageService>();
            service.AddTransient<ICloudinaryService, CloudinaryService>();
            service.AddTransient<IAuthenticationService, AuthenticationService>();
            service.AddTransient<IOtpService, OtpService>();
            service.AddTransient<IRepairRequestService, RepairRequestService>();
            service.AddTransient<IAppointmentService, AppointmentService>();
            service.AddTransient<ITechniqueService, TechniqueService>();
            service.AddTransient<IIssueService, IssueService>();
            service.AddTransient<IAppointmentAssignService, AppointmentAssignService>();
            service.AddTransient<ISlotService, SlotService>();
            service.AddTransient<IInvoiceService, Service.Services.Implements.InvoiceService>();
            service.AddTransient<IInspectionReporService, InspectionReporService>();
            service.AddTransient<IReportApprovalService, ReportApprovalService>();
            service.AddTransient<INotificationService, NotificationService>();
            service.AddHttpClient<IFCMService, FCMService>();
            service.AddTransient<IRepairReportService, RepairReportService>();
            service.AddTransient<IAccessoryService, AccessoryService>();
            service.AddSingleton<IS3FileService, S3FileService>();
            service.AddTransient<IPayOSWebhookService, PayOSWebhookService>();
            service.AddTransient<IContractService, ContractService>();
            service.AddScoped<ITransactionService, TransactionService>();



            service.AddHostedService<NotificationBackgroundService>();


            return service;
        }
    }
}
