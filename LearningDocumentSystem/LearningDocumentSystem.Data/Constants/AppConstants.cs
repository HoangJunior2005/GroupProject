namespace LearningDocumentSystem.Common.Constants
{
    public static class AppConstants
    {
        // File Upload
        public const string UploadFolder = "uploads";

        // Embedding (giả lập OpenAI ada-002)
        public const int EmbeddingDimension = 1536;

        public const string SessionUserId = "UserId";
        public const string SessionUsername = "Username";
        public const string SessionFullName = "FullName";
        public const string SessionRoles = "UserRoles";

        // Roles
        public const string RoleAdmin = "Admin";
        public const string RoleTeacher = "Teacher";
        public const string RoleStudent = "Student";

        // Index Status strings (theo DB schema)
        public const string StatusPending = "Pending";
        public const string StatusProcessing = "Processing";
        public const string StatusIndexed = "Indexed";
        public const string StatusFailed = "Failed";

        // Cookie Auth
        public const string AuthCookieName = "LDS.Auth";
        public const string LoginPath = "/Account/Login";
        public const string AccessDeniedPath = "/Account/AccessDenied";
    }
}
