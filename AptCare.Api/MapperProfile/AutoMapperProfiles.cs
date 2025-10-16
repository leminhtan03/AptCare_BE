using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.Apartment;
using AptCare.Service.Dtos.Account;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Dtos.ChatDtos;
using AptCare.Service.Dtos.UserDtos;
using AptCare.Service.Dtos.WorkSlotDtos;
using AutoMapper;
namespace AptCare.Api.MapperProfile
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<User, UserDto>()
               .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
               .ForMember(
                dest => dest.Apartments,
                opt => opt.MapFrom(src =>
                    src.UserApartments.Select(ua => new ApartmentForUserDto
                    {
                        RoomNumber = ua.Apartment.RoomNumber,
                        RoleInApartment = ua.RoleInApartment.ToString(),
                        RelationshipToOwner = ua.RelationshipToOwner
                    })
                ))
               .ForMember(dest => dest.AccountInfo, opt => opt.MapFrom(src => src.Account));
            CreateMap<User, GetOwnProfileDto>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.Role , opt => opt.MapFrom(src => src.Account.Role.ToString()))
                .ForMember(
                dest => dest.Apartments,
                    opt => opt.MapFrom(
                        src => src.UserApartments.Select(
                            ua => new ApartmentForUserProfileDto
                            {
                                RoomNumber = ua.Apartment.RoomNumber,
                                RoleInApartment = ua.RoleInApartment.ToString(),
                                RelationshipToOwner = ua.RelationshipToOwner,
                                Floor = ua.Apartment.Floor.FloorNumber,
                            })
                ));

            CreateMap<Account, AccountForAdminDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
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
               .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ActiveStatus.Active));
            CreateMap<FloorUpdateDto, Floor>()
               .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            //APARTMENT
            CreateMap<Apartment, ApartmentDto>()
               .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
               .ForMember(dest => dest.Floor, opt => opt.MapFrom(src => src.Floor.FloorNumber.ToString()))
               .ForMember(dest => dest.Users, opt => opt.MapFrom(src => src.UserApartments));
            CreateMap<UserApartment, UserInApartmentDto>()
               .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))

               .ForMember(dest => dest.User, opt => opt.MapFrom(src => new UserDto
               {
                   UserId = src.User.UserId,
                   FirstName = src.User.FirstName,
                   LastName = src.User.LastName,
                   Email = src.User.Email,
                   PhoneNumber = src.User.PhoneNumber,
                   CitizenshipIdentity = src.User.CitizenshipIdentity,
                   Birthday = src.User.Birthday,
                   Apartments = null,
                   Status = src.User.Status.ToString(),
                   AccountInfo = src.User.Account == null ? null : new AccountForAdminDto
                   {
                       AccountId = src.User.Account.AccountId,
                       Username = src.User.Account.Username,
                       Role = src.User.Account.Role.ToString(),
                       EmailConfirmed = src.User.Account.EmailConfirmed,
                       LockoutEnabled = src.User.Account.LockoutEnabled,
                       LockoutEnd = src.User.Account.LockoutEnd
                   }
               }));
            CreateMap<ApartmentCreateDto, Apartment>()
               .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ApartmentStatus.Active));
            CreateMap<ApartmentUpdateDto, Apartment>()
               .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            //COMMON AREA
            CreateMap<CommonArea, CommonAreaDto>()
               .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
               .ForMember(dest => dest.Floor, opt => opt.MapFrom(src => src.Floor.FloorNumber.ToString()));
            CreateMap<CommonAreaCreateDto, CommonArea>()
               .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ActiveStatus.Active));
            CreateMap<CommonAreaUpdateDto, CommonArea>()
               .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            //WORK SLOT
            CreateMap<WorkSlotUpdateDto, WorkSlot>()
               .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            CreateMap<WorkSlot, TechnicianWorkSlotDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

            //CONVERSATION
            //CreateMap<Us, Conversation>()
            //   .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            //MESSAGE
            CreateMap<TextMessageCreateDto, Message>()
               .ForMember(dest => dest.Status, opt => opt.MapFrom(src => MessageStatus.Sent))
               .ForMember(dest => dest.Type, opt => opt.MapFrom(src => MessageType.Text))
               .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow.AddHours(7)));
            CreateMap<Message, MessageDto>()
               .ForMember(dest => dest.SenderName, opt => opt.MapFrom(src => src.Sender.FirstName + " " + src.Sender.LastName))
               //.ForMember(dest => dest.SenderAvatar, opt => opt.MapFrom(src => src.Sender.AvatarUrl))
               .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
               .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
               .ForMember(dest => dest.ReplyType, opt => opt.MapFrom(src => src.ReplyMessage != null ? src.ReplyMessage.Type.ToString() : null))
               .ForMember(dest => dest.ReplyContent, opt => opt.MapFrom(src => src.ReplyMessage != null ? src.ReplyMessage.Content : null))
               .ForMember(dest => dest.ReplySenderName, opt => opt.MapFrom(src => src.ReplyMessage != null
                                                                                ? src.ReplyMessage.Sender.FirstName + " " + src.ReplyMessage.Sender.LastName
                                                                                : null))
               .ForMember(dest => dest.IsMine,
                    opt => opt.MapFrom((src, dest, destMember, context) =>
                        src.SenderId == (int)context.Items["CurrentUserId"]));

            CreateMap<Account, AccountForAdminDto>()
               .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));
        }
    }
}
