using System.ComponentModel.DataAnnotations;
using LearningDocumentSystem.Business.DTOs;

namespace LearningDocumentSystem.Web.ViewModels
{
    public class ChapterFormViewModel
    {
        public int ChapterID { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn môn học")]
        [Display(Name = "Môn học")]
        public int SubjectID { get; set; }

        [Required(ErrorMessage = "Số chương không được để trống")]
        [Range(1, 100)]
        [Display(Name = "Số chương")]
        public int ChapterNumber { get; set; }

        [Required(ErrorMessage = "Tên chương không được để trống")]
        [MaxLength(255)]
        [Display(Name = "Tên chương")]
        public string ChapterName { get; set; } = string.Empty;

        public IEnumerable<SubjectDto> Subjects { get; set; } = [];
    }
}