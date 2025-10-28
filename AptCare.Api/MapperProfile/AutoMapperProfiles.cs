using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.Apartment;
using AptCare.Service.Dtos.Account;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Dtos.ChatDtos;
using AptCare.Service.Dtos.RepairRequestDtos;
using AptCare.Service.Dtos.IssueDto;
using AptCare.Service.Dtos.TechniqueDto;
using AptCare.Service.Dtos.UserDtos;
using AptCare.Service.Dtos.WorkSlotDtos;
using AutoMapper;
using AptCare.Service.Dtos.AppointmentDtos;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.SlotDtos;

namespace AptCare.Api.MapperProfile
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            // ===== Account =====
            CreateMap<Account, AccountForAdminDto>()
                .ForMember(d => d.Role, o => o.MapFrom(s => s.Role.ToString()))
                .ForMember(d => d.LockoutEnabled, o => o.MapFrom(s => s.LockoutEnabled))
                .ForMember(d => d.LockoutEnd, o => o.MapFrom(s => s.LockoutEnd));
            CreateMap<Account, AccountDto>()
                .ForMember(d => d.Role, o => o.MapFrom(s => s.Role.ToString()));

            // ===== UserApartment -> ApartmentForUser* (map con, dùng lại) =====
            CreateMap<UserApartment, ApartmentForUserDto>()
                .ForMember(d => d.Room, o => o.MapFrom(s => s.Apartment.Room))
                .ForMember(d => d.RoleInApartment, o => o.MapFrom(s => s.RoleInApartment.ToString()))
                .ForMember(d => d.RelationshipToOwner, o => o.MapFrom(s => s.RelationshipToOwner));

            CreateMap<UserApartment, ApartmentForUserProfileDto>()
                .ForMember(d => d.Room, o => o.MapFrom(s => s.Apartment.Room))
                .ForMember(d => d.RoleInApartment, o => o.MapFrom(s => s.RoleInApartment.ToString()))
                .ForMember(d => d.RelationshipToOwner, o => o.MapFrom(s => s.RelationshipToOwner))
                .ForMember(d => d.Floor, o => o.MapFrom(s => s.Apartment.Floor.FloorNumber));

            // ===== User -> UserDto / GetOwnProfileDto =====
            CreateMap<User, UserDto>()
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.Apartments, o => o.MapFrom(s => s.UserApartments))
                .ForMember(d => d.AccountInfo, o => o.MapFrom(s => s.Account));

            CreateMap<User, UserBasicDto>();

            CreateMap<User, GetOwnProfileDto>()
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.Role, o => o.MapFrom(s => s.Account.Role.ToString()))
                .ForMember(d => d.Apartments, o => o.MapFrom(s => s.UserApartments))
                .ForMember(d => d.Techniques, o => o.MapFrom(s => s.TechnicianTechniques));

            // Cho tạo/cập nhật User
            CreateMap<CreateUserDto, User>();
            CreateMap<UpdateUserDto, User>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // ===== Floor =====
            CreateMap<Floor, FloorDto>()
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));
            CreateMap<FloorCreateDto, Floor>()
                .ForMember(d => d.Status, o => o.MapFrom(s => ActiveStatus.Active));
            CreateMap<FloorUpdateDto, Floor>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // ===== Apartment =====
            CreateMap<Apartment, ApartmentDto>()
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.Floor, o => o.MapFrom(s => s.Floor.FloorNumber.ToString()))
                .ForMember(d => d.Users, o => o.MapFrom(s => s.UserApartments));

            CreateMap<UserApartment, UserInApartmentDto>()
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.User, o => o.MapFrom(s => new UserDto
                {
                    UserId = s.User.UserId,
                    FirstName = s.User.FirstName,
                    LastName = s.User.LastName,
                    Email = s.User.Email,
                    PhoneNumber = s.User.PhoneNumber,
                    CitizenshipIdentity = s.User.CitizenshipIdentity,
                    Birthday = s.User.Birthday,
                    Apartments = null, // tránh vòng lặp
                    Status = s.User.Status.ToString(),
                    AccountInfo = s.User.Account == null ? null : new AccountForAdminDto
                    {
                        AccountId = s.User.Account.AccountId,
                        Username = s.User.Account.Username,
                        Role = s.User.Account.Role.ToString(),
                        EmailConfirmed = s.User.Account.EmailConfirmed,
                        LockoutEnabled = s.User.Account.LockoutEnabled,
                        LockoutEnd = s.User.Account.LockoutEnd
                    }
                }));

            CreateMap<ApartmentCreateDto, Apartment>()
                .ForMember(d => d.Status, o => o.MapFrom(s => ApartmentStatus.Active));
            CreateMap<ApartmentUpdateDto, Apartment>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // ===== CommonArea =====
            CreateMap<CommonArea, CommonAreaDto>()
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.Floor, o => o.MapFrom(s => s.Floor.FloorNumber.ToString()));
            CreateMap<CommonAreaCreateDto, CommonArea>()
                .ForMember(d => d.Status, o => o.MapFrom(s => ActiveStatus.Active));
            CreateMap<CommonAreaUpdateDto, CommonArea>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // ===== WorkSlot =====
            CreateMap<WorkSlotUpdateDto, WorkSlot>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            CreateMap<WorkSlot, TechnicianWorkSlotDto>()
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));

            // ===== Message / Chat =====
            CreateMap<TextMessageCreateDto, Message>()
                .ForMember(d => d.Status, o => o.MapFrom(s => MessageStatus.Sent))
                .ForMember(d => d.Type, o => o.MapFrom(s => MessageType.Text))
                .ForMember(d => d.CreatedAt, o => o.MapFrom(s => DateTime.UtcNow.AddHours(7)));

            CreateMap<Message, MessageDto>()
                .ForMember(d => d.Slug, o => o.MapFrom(s => s.Conversation.Slug))
                .ForMember(d => d.SenderName, o => o.MapFrom(s => s.Sender.FirstName + " " + s.Sender.LastName))
                .ForMember(d => d.Type, o => o.MapFrom(s => s.Type.ToString()))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.ReplyType, o => o.MapFrom(s => s.ReplyMessage != null ? s.ReplyMessage.Type.ToString() : null))
                .ForMember(d => d.ReplyContent, o => o.MapFrom(s => s.ReplyMessage != null ? s.ReplyMessage.Content : null))
                .ForMember(d => d.ReplySenderName, o => o.MapFrom(s => s.ReplyMessage != null
                                                        ? s.ReplyMessage.Sender.FirstName + " " + s.ReplyMessage.Sender.LastName
                                                        : null))
                .ForMember(d => d.IsMine, o => o.Ignore());

            CreateMap<Account, AccountForAdminDto>()
               .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));

            //REPAIR REQUEST
            CreateMap<RepairRequest, RepairRequestDto>()
                .ForMember(d => d.ChildRequestIds, o => o.MapFrom(s => s.ChildRequests != null ? s.ChildRequests.Select(x => x.RepairRequestId) : null));

            CreateMap<RepairRequest, RepairRequestBasicDto>();
            CreateMap<RequestTracking, RequestTrackingDto>()
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));

            //APOINTMENT
            CreateMap<Appointment, AppointmentDto>()
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.Technicians, o => o.MapFrom(s => s.AppointmentAssigns.Select(x => x.Technician)));
            CreateMap<AppointmentCreateDto, Appointment>()
                .ForMember(d => d.Status, o => o.MapFrom(s => AppointmentStatus.Pending))
                .ForMember(d => d.CreatedAt, o => o.MapFrom(s => DateTime.UtcNow.AddHours(7)));
            CreateMap<AppointmentUpdateDto, Appointment>();

            CreateMap<RepairRequestNormalCreateDto, RepairRequest>()
               .ForMember(dest => dest.IsEmergency, opt => opt.MapFrom(src => false))
               .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow.AddHours(7)));
            CreateMap<RepairRequestEmergencyCreateDto, RepairRequest>()
               .ForMember(dest => dest.IsEmergency, opt => opt.MapFrom(src => true))
                .ForMember(d => d.CreatedAt, o => o.MapFrom(s => DateTime.UtcNow.AddHours(7)));

            // ===== Issue =====
            CreateMap<IssueCreateDto, Issue>()
                .ForMember(d => d.Status, o => o.MapFrom(s => ActiveStatus.Active));
            CreateMap<Issue, IssueListItemDto>()
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.TechniqueName, o => o.MapFrom(s => s.Technique.Name));
            CreateMap<IssueUpdateDto, Issue>()
                .ForMember(d => d.Status, o => o.MapFrom(s => Enum.Parse<ActiveStatus>(s.Status)));
            CreateMap<Technique, TechniqueListItemDto>()
                .ForMember(d => d.IssueCount, o => o.MapFrom(s => s.Issues.Count));
            CreateMap<TechniqueCreateDto, Technique>();
            CreateMap<TechniqueUpdateDto, Technique>();
            CreateMap<TechnicianTechnique, TechniqueResponseDto>()
                .ForMember(e => e.TechniqueName, o => o.MapFrom(s => s.Technique.Name))
                .ForMember(e => e.Description, o => o.MapFrom(s => s.Technique.Description));

            //MEDIA
            CreateMap<Media, MediaDto>();

            //SLOT
            CreateMap<Slot, SlotDto>()
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));
            CreateMap<SlotCreateDto, Slot>()
                .ForMember(d => d.Status, o => o.MapFrom(s => ActiveStatus.Active))
                .ForMember(d => d.LastUpdated, o => o.MapFrom(s => DateTime.UtcNow.AddHours(7)));
            CreateMap<SlotUpdateDto, Slot>()
                .ForMember(d => d.LastUpdated, o => o.MapFrom(s => DateTime.UtcNow.AddHours(7)));
        }
    }
}
