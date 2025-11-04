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

        [Column(TypeName = "timestamp without time zone")]
        public DateTime CreatedAt { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime ExpiresAt { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime? VerifiedAt { get; set; }

        public OTPStatus Status { get; set; }
    }
}
