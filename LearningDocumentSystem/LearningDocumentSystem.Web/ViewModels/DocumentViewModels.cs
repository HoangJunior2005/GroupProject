using System.ComponentModel.DataAnnotations;
using LearningDocumentSystem.Business.DTOs;

namespace LearningDocumentSystem.Web.ViewModels
{
    public class DocumentUploadViewModel
    {
        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        [MaxLength(255)]
        [Display(Name = "Tiêu đề tài liệu")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn chương")]
        [Display(Name = "Chương")]
        public int ChapterId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn file")]
        [Display(Name = "File tài liệu")]
        public IFormFile? File { get; set; }

        public IEnumerable<SubjectDto> Subjects { get; set; } = [];
        public IEnumerable<ChapterDto> Chapters { get; set; } = [];
        public int? SelectedSubjectId { get; set; }
    }
}