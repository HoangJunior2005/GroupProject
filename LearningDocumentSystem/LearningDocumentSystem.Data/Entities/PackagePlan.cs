using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearningDocumentSystem.Entities.Models
{
    public class PackagePlan
    {
        [Key]
        [Column("PackagePlanID")]
        public int PackagePlanID { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int? DailyMessageLimit { get; set; }

        [Required]
        public string AllowedProvidersJson { get; set; } = "[]";

        [Required]
        public string FeaturesJson { get; set; } = "[]";

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; }
    }
}
