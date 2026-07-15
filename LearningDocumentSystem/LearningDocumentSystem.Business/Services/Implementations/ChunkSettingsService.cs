using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Common.Constants;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using LearningDocumentSystem.Entities.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LearningDocumentSystem.Business.Services.Implementations
{
    public class ChunkSettingsService : IChunkSettingsService
    {
        private readonly IUnitOfWork _uow;
        private readonly IConfiguration _config;
        private readonly ILogger<ChunkSettingsService> _logger;

        // Danh sách strategy hợp lệ
        private static readonly HashSet<string> ValidStrategies =
            new(StringComparer.OrdinalIgnoreCase) { "FixedSize", "Paragraph", "Recursive" };

        public ChunkSettingsService(IUnitOfWork uow, IConfiguration config, ILogger<ChunkSettingsService> logger)
        {
            _uow    = uow;
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Lấy cấu hình chunking chung toàn hệ thống (do Admin thiết lập).
        /// </summary>
        public async Task<ChunkSettingsDto> GetGlobalSettingsAsync()
        {
            var setting = await _uow.SystemChunkSettings.FirstOrDefaultAsync(
                s => s.User.UserRoles.Any(ur => ur.Role.RoleName == AppConstants.RoleAdmin));

            if (setting != null)
            {
                return new ChunkSettingsDto
                {
                    Strategy       = setting.Strategy,
                    ChunkSize      = setting.ChunkSize,
                    ChunkOverlap   = setting.ChunkOverlap,
                    MinChunkLength = setting.MinChunkLength,
                    UpdatedAt      = setting.UpdatedAt
                };
            }

            // Fallback: default từ appsettings.json
            return new ChunkSettingsDto
            {
                Strategy       = _config.GetValue<string>("AppSettings:ChunkStrategy", "Recursive") ?? "Recursive",
                ChunkSize      = _config.GetValue<int>("AppSettings:ChunkSize", 800),
                ChunkOverlap   = _config.GetValue<int>("AppSettings:ChunkOverlap", 100),
                MinChunkLength = _config.GetValue<int>("AppSettings:MinChunkLength", 50),
                UpdatedAt      = null
            };
        }

        /// <summary>
        /// Lưu (hoặc cập nhật) cấu hình chunking hệ thống (do Admin thiết lập).
        /// </summary>
        public async Task SaveSettingsAsync(
            int adminId, string strategy, int chunkSize, int chunkOverlap, int minChunkLength)
        {
            // Validate strategy
            if (!ValidStrategies.Contains(strategy))
                throw new ArgumentException($"Strategy '{strategy}' không hợp lệ. Chọn: FixedSize, Paragraph, Recursive.");

            // Validate ranges
            if (chunkSize < 100 || chunkSize > 10000)
                throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk Size phải từ 100 đến 10000 ký tự.");
            if (chunkOverlap < 0 || chunkOverlap >= chunkSize)
                throw new ArgumentOutOfRangeException(nameof(chunkOverlap), "Chunk Overlap phải từ 0 đến (Chunk Size - 1).");
            if (minChunkLength < 10 || minChunkLength > chunkSize)
                throw new ArgumentOutOfRangeException(nameof(minChunkLength), "Min Chunk Length phải từ 10 đến Chunk Size.");

            var existingSetting = await _uow.SystemChunkSettings.FirstOrDefaultAsync(
                s => s.User.UserRoles.Any(ur => ur.Role.RoleName == AppConstants.RoleAdmin));

            if (existingSetting != null)
            {
                existingSetting.Strategy       = strategy;
                existingSetting.ChunkSize      = chunkSize;
                existingSetting.ChunkOverlap   = chunkOverlap;
                existingSetting.MinChunkLength = minChunkLength;
                existingSetting.UpdatedAt      = DateTime.UtcNow;

                _uow.SystemChunkSettings.Update(existingSetting);
                await _uow.SaveChangesAsync();

                _logger.LogInformation(
                    "Admin updated global chunk settings: strategy={Strategy}, size={Size}, overlap={Overlap}, min={Min}",
                    strategy, chunkSize, chunkOverlap, minChunkLength);
            }
            else
            {
                var setting = new SystemChunkSetting
                {
                    UserId         = adminId,
                    Strategy       = strategy,
                    ChunkSize      = chunkSize,
                    ChunkOverlap   = chunkOverlap,
                    MinChunkLength = minChunkLength,
                    UpdatedAt      = DateTime.UtcNow
                };

                await _uow.SystemChunkSettings.UpsertAsync(setting);
                await _uow.SaveChangesAsync();

                _logger.LogInformation(
                    "Admin created global chunk settings: strategy={Strategy}, size={Size}, overlap={Overlap}, min={Min}",
                    strategy, chunkSize, chunkOverlap, minChunkLength);
            }
        }
    }
}
