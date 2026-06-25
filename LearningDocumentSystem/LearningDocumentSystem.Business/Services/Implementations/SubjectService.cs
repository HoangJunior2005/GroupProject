using AutoMapper;
using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Common.Exceptions;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using LearningDocumentSystem.Entities.Models;
using Microsoft.Extensions.Logging;

namespace LearningDocumentSystem.Business.Services.Implementations
{
    public class SubjectService : ISubjectService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<SubjectService> _logger;
        private readonly INotificationService _notificationService;

        public SubjectService(IUnitOfWork uow, IMapper mapper, ILogger<SubjectService> logger, INotificationService notificationService)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<IEnumerable<SubjectDto>> GetAllAsync()
        {
            var subjects = await _uow.Subjects.GetAllAsync();
            return _mapper.Map<IEnumerable<SubjectDto>>(subjects);
        }

        public async Task<SubjectDto?> GetByIdAsync(int id)
        {
            var subject = await _uow.Subjects.GetByIdAsync(id);
            return subject == null ? null : _mapper.Map<SubjectDto>(subject);
        }

        public async Task<SubjectDto?> GetWithChaptersAsync(int id)
        {
            var subject = await _uow.Subjects.GetWithChaptersAsync(id);
            return subject == null ? null : _mapper.Map<SubjectDto>(subject);
        }

        public async Task<SubjectDto> CreateAsync(CreateSubjectDto dto)
        {
            if (await _uow.Subjects.IsCodeExistsAsync(dto.SubjectCode))
                throw new BusinessException($"Mã học phần '{dto.SubjectCode}' đã tồn tại.");

            var subject = _mapper.Map<Subject>(dto);
            subject.CreatedAt = DateTime.UtcNow;
            subject.SubjectLeaderID = dto.SubjectLeaderID;
            await _uow.Subjects.AddAsync(subject);
            await _uow.SaveChangesAsync();

            _logger.LogInformation("Subject created: {Code}", dto.SubjectCode);
            
            var result = _mapper.Map<SubjectDto>(subject);
            await _notificationService.SendNotificationAsync("SubjectCreated", result);
            return result;
        }

        public async Task<SubjectDto> UpdateAsync(UpdateSubjectDto dto)
        {
            var subject = await _uow.Subjects.GetByIdAsync(dto.SubjectID)
                ?? throw new NotFoundException("Subject", dto.SubjectID);

            if (await _uow.Subjects.IsCodeExistsAsync(dto.SubjectCode, dto.SubjectID))
                throw new BusinessException($"Mã học phần '{dto.SubjectCode}' đã tồn tại.");

            subject.SubjectName = dto.SubjectName;
            subject.SubjectCode = dto.SubjectCode;
            subject.SubjectLeaderID = dto.SubjectLeaderID;
            _uow.Subjects.Update(subject);
            await _uow.SaveChangesAsync();

            var result = _mapper.Map<SubjectDto>(subject);
            await _notificationService.SendNotificationAsync("SubjectUpdated", result);
            return result;
        }

        public async Task DeleteAsync(int id)
        {
            var subject = await _uow.Subjects.GetWithChaptersAsync(id)
                ?? throw new NotFoundException("Subject", id);

            if (subject.Chapters.Any(c => c.Documents.Any()))
            {
                throw new BusinessException("Không được phép xóa môn học này vì đã có tài liệu.");
            }

            _uow.Subjects.Remove(subject);
            await _uow.SaveChangesAsync();
            _logger.LogInformation("Subject deleted: {Id}", id);
            await _notificationService.SendNotificationAsync("SubjectDeleted", new { subjectId = id });
        }
    }
}