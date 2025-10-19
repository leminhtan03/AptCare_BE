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

namespace AptCare.Service.Dtos.RepairRequestDtos
{
    public class RepairRequestDto
    {
        public int RepairRequestId { get; set; }
        public UserDto User { get; set; } = null!;
        public ApartmentDto? Apartment { get; set; }
        public RepairRequestDto? ParentRequest { get; set; }
        public Issue? Issue { get; set; }
        public MaintenanceRequest? MaintenanceRequest { get; set; }
        public string Object { get; set; } = null!;
        public string Description { get; set; } = null!;
        public bool IsEmergency { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? AcceptanceTime { get; set; }
        public RequestStatus Status { get; set; }
        public List<RepairRequestDto>? ChildRequests { get; set; }
        public List<RequestTracking>? RequestTrackings { get; set; }
    }

    public class RequestTrackingDto
    {
        public int RequestTrackingId { get; set; }
        public RequestStatus Status { get; set; }
        public string? Note { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public UserDto UpdatedByUser { get; set; } = null!;
    }
}
