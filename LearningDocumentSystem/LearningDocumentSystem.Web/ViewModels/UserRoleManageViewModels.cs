using LearningDocumentSystem.Business.DTOs;

namespace LearningDocumentSystem.Web.ViewModels
{
    public class UserRoleItemViewModel
    {
        public int UserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool CanUpload { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<int> AssignedRoleIds { get; set; } = new();
        public List<int> AssignedSubjectIds { get; set; } = new();
    }

    public class UserRoleManageViewModel
    {
        public IEnumerable<RoleDto> Roles { get; set; } = Enumerable.Empty<RoleDto>();
        public IEnumerable<UserRoleItemViewModel> Users { get; set; } = Enumerable.Empty<UserRoleItemViewModel>();
        public IEnumerable<SubjectDto> AllSubjects { get; set; } = Enumerable.Empty<SubjectDto>();
    }
}
