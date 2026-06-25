using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearningDocumentSystem.Entities.Models
{
    public class Subject
    {
        [Key]
        [Column("SubjectID")]
        public int SubjectID { get; set; }

        [Required]
        [MaxLength(255)]
        public string SubjectName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string SubjectCode { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("SubjectLeaderID")]
        public int? SubjectLeaderID { get; set; }

        // Navigation properties
        public virtual ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
        public virtual User? SubjectLeader { get; set; }
    }
}
