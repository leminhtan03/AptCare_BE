using AptCare.Repository.Enum.OTPEnum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AptCare.Repository.Entities
{
    public class AccountOTPHistory
    {
        [Key]
        public int OTPId { get; set; }
        [ForeignKey("Account")]
        public int AccountId { get; set; }
        public Account Account { get; set; }

        [Required]
        public string OTPCode { get; set; }

        public OTPType OTPType { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime? VerifiedAt { get; set; }

        public OTPStatus Status { get; set; }
    }
}
