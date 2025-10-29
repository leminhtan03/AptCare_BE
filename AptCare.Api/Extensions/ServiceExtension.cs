using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Services.Implements;
using AptCare.Service.Services.Interfaces;
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


            return service;
        }
    }
}
