using AptCare.Repository.Enum.TokenEnum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AptCare.Repository.Entities
{
    public class AccountToken
    {
        [Key]
        public int TokenId { get; set; }
        [ForeignKey("Account")]
        public int AccountId { get; set; }
        [Required]
        public string Token { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public TokenStatus Status { get; set; }

        public TokenType TokenType { get; set; }

        public string DeviceInfo { get; set; }
        public Account Account { get; set; }
    }
}
