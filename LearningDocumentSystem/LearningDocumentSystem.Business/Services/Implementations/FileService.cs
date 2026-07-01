using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Common.Constants;
using LearningDocumentSystem.Common.Exceptions;
using LearningDocumentSystem.Common.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace LearningDocumentSystem.Business.Services.Implementations
{
    public class FileService : IFileService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<FileService> _logger;

        public FileService(IConfiguration config, ILogger<FileService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string uploadFolder)
        {
            if (file == null || file.Length == 0)
                throw new InvalidFileException("File không hợp lệ.");

            if (!FileHelper.IsAllowedExtension(file.FileName))
                throw new InvalidFileException(AppMessages.MsgInvalidFileType);

            var maxFileSizeMB = _config.GetValue<long>("AppSettings:MaxFileSizeMB", 50);
            var maxFileSizeBytes = maxFileSizeMB * 1024 * 1024;
            if (file.Length > maxFileSizeBytes)
                throw new InvalidFileException(AppMessages.MsgFileSizeExceeded);

            // Tạo tên file unique
            var uniqueFileName = FileHelper.GenerateStoragePath(file.FileName);
            var fullPath = Path.Combine(uploadFolder, uniqueFileName);

            // Đảm bảo thư mục tồn tại
            Directory.CreateDirectory(uploadFolder);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            _logger.LogInformation("File saved: {FileName}", uniqueFileName);
            return uniqueFileName; // Chỉ trả về tên file, không phải full path
        }

        public void DeleteFile(string storagePath, string uploadFolder)
        {
            var fullPath = Path.Combine(uploadFolder, storagePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("File deleted: {Path}", storagePath);
            }
        }
    }
}
