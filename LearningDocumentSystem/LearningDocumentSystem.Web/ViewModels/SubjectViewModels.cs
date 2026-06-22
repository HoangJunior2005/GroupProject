using System.ComponentModel.DataAnnotations;
using LearningDocumentSystem.Business.DTOs;

namespace LearningDocumentSystem.Web.ViewModels
{
    public class SubjectFormViewModel
    {
        public int SubjectID { get; set; }

        [Required(ErrorMessage = "Tên môn học không được để trống")]
        [MaxLength(255)]
        [Display(Name = "Tên môn học")]
        public string SubjectName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã học phần không được để trống")]
        [MaxLength(50)]
        [RegularExpression(@"^[A-Z0-9]+$", ErrorMessage = "Mã học phần chỉ gồm chữ hoa và số (VD: INF205)")]
        [Display(Name = "Mã học phần")]
        public string SubjectCode { get; set; } = string.Empty;
    }
}