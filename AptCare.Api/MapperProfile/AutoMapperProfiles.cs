using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Service.Dtos.Account;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Dtos.UserDtos;
using AutoMapper;
namespace AptCare.Api.MapperProfile
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<User, UserDto>()
               .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
            CreateMap<CreateUserDto, User>();
            CreateMap<UpdateUserDto, User>()
               .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            CreateMap<Account, AccountDto>()
               .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));

            //FLOOR
            CreateMap<Floor, FloorDto>()
               .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
            CreateMap<FloorCreateDto, Floor>()
               .ForMember(dest => dest.Status, opt => opt.MapFrom(src => nameof(ActiveStatus.Active)));
            CreateMap<FloorUpdateDto, Floor>()
               .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            //APARTMENT
            CreateMap<Apartment, ApartmentDto>()
               .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
        }
    }
}
