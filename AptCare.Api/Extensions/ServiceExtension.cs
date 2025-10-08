using AptCare.Repository;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Services.Implements;
using AptCare.Service.Services.Interfaces;
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

            service.AddTransient<IAccountService, AccountService>();
            service.AddTransient<IUserService, UserService>();
            //service.AddTransient<ITokenService, TokenService>();
            service.AddTransient<IFloorService, FloorService>();
            service.AddTransient<IApartmentService, ApartmentService>();
            service.AddTransient<IUserService, UserService>();

            return service;
        }
    }
}
