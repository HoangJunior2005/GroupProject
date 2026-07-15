using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearningDocumentSystem.Entities.Models
{
    public class PaymentTransaction
    {
        [Key]
        [Column("TransactionID")]
        public int TransactionID { get; set; }

        [Required]
        public int UserID { get; set; }

        [ForeignKey("UserID")]
        public virtual User User { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string PlanCode { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(100)]
        public string TransactionReference { get; set; } = string.Empty;

        public bool IsSuccess { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
