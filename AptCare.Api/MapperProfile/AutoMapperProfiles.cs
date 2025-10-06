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
               .ForMember(dest => dest.Status, opt => opt.MapFrom(src => nameof(src.Status)))
               .ForMember(
                dest => dest.Apartments,
                opt => opt.MapFrom(src =>
                    src.UserApartments.Select(ua => new ApartmentForUserDto
                    {
                        RoomNumber = ua.Apartment.RoomNumber,
                        RoleInApartment = nameof(ua.RoleInApartment),
                        RelationshipToOwner = ua.RelationshipToOwner
                    })
                ))
               .ForMember(dest => dest.AccountInfo, opt => opt.MapFrom(src => src.Account));
            CreateMap<Account, AccountForAdminDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => nameof(src.Role)))
            .ForMember(dest => dest.LockoutEnabled, opt => opt.MapFrom(src => src.LockoutEnabled))
            .ForMember(dest => dest.LockoutEnd, opt => opt.MapFrom(src => src.LockoutEnd));

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
            CreateMap<Account, AccountForAdminDto>()
               .ForMember(dest => dest.Role, opt => opt.MapFrom(src => nameof(src.Role)));
        }
    }
}
