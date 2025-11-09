using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Dtos.UserDtos;
using AptCare.Service.Dtos.AppointmentDtos;
using AptCare.Service.Dtos.Account;
using AptCare.Service.Dtos.IssueDto;

namespace AptCare.Service.Dtos.RepairRequestDtos
{
    public class RepairRequestDto
    {
        public int RepairRequestId { get; set; }
        public string Object { get; set; } = null!;
        public string Description { get; set; } = null!;
        public bool IsEmergency { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? AcceptanceTime { get; set; }
        public int? ParentRequestId { get; set; }
        public ApartmentDto Apartment { get; set; } = null!;
        public IssueListItemDto? Issue { get; set; }
        public int? MaintenanceRequestId { get; set; }
        public List<int>? ChildRequestIds { get; set; }
        public List<MediaDto>? Medias { get; set; }
        public string Status { get; set; } = null!;
    }

    public class RepairRequestDetailDto
    {
        public int RepairRequestId { get; set; }
        public string Object { get; set; } = null!;
        public string Description { get; set; } = null!;
        public bool IsEmergency { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? AcceptanceTime { get; set; }
        public int? ParentRequestId { get; set; }
        public UserBasicDto User { get; set; } = null!;
        public ApartmentDto Apartment { get; set; } = null!;
        public IssueListItemDto? Issue { get; set; }
        public int? MaintenanceRequestId { get; set; }
        public List<int>? ChildRequestIds { get; set; }
        public List<MediaDto>? Medias { get; set; }
        public List<RequestTrackingDto>? RequestTrackings { get; set; }
        public List<AppointmentBasicDto>? Appointments { get; set; }
    }

    public class RequestTrackingDto
    {
        public int RequestTrackingId { get; set; }
        public string Status { get; set; } = null!;
        public string? Note { get; set; }
        public DateTime UpdatedAt { get; set; }
        public UserBasicDto UpdatedByUser { get; set; } = null!;
    }

    public class RepairRequestBasicDto
    {
        public int RepairRequestId { get; set; }
        public string Object { get; set; } = null!;
        public string Description { get; set; } = null!;
        public bool IsEmergency { get; set; }
        public IssueListItemDto? Issue { get; set; }
        public ApartmentDto Apartment { get; set; } = null!;
        public List<MediaDto>? Medias { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserBasicDto? CreateUser { get; set; } = null!;

    }
}
