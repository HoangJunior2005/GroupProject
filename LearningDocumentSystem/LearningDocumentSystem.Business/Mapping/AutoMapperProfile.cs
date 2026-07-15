using AutoMapper;
using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Entities.Models;
using LearningDocumentSystem.Common.Helpers;

namespace LearningDocumentSystem.Business.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // User
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Roles,
                    opt => opt.MapFrom(src => src.UserRoles.Select(ur => ur.Role.RoleName).ToList()));

            CreateMap<Role, RoleDto>();
            CreateMap<AllowedEmail, AllowedEmailDto>();

            // Subject
            CreateMap<Subject, SubjectDto>()
                .ForMember(dest => dest.ChapterCount,
                    opt => opt.MapFrom(src => src.Chapters.Count))
                .ForMember(dest => dest.DocumentCount,
                    opt => opt.MapFrom(src => src.Chapters.Sum(c => c.Documents.Count)))
                .ForMember(dest => dest.SubjectLeaderName,
                    opt => opt.MapFrom(src => src.SubjectLeader != null ? src.SubjectLeader.FullName : null));
            CreateMap<CreateSubjectDto, Subject>();
            CreateMap<UpdateSubjectDto, Subject>();

            // Chapter
            CreateMap<Chapter, ChapterDto>()
                .ForMember(dest => dest.SubjectName,
                    opt => opt.MapFrom(src => src.Subject.SubjectName))
                .ForMember(dest => dest.DocumentCount,
                    opt => opt.MapFrom(src => src.Documents.Count));
            CreateMap<CreateChapterDto, Chapter>();
            CreateMap<UpdateChapterDto, Chapter>();

            // Document
            CreateMap<Document, DocumentDto>()
                .ForMember(dest => dest.ChapterName,
                    opt => opt.MapFrom(src => src.Chapter.ChapterName))
                .ForMember(dest => dest.ChapterNumber,
                    opt => opt.MapFrom(src => src.Chapter.ChapterNumber))
                .ForMember(dest => dest.SubjectID,
                    opt => opt.MapFrom(src => src.Chapter.SubjectID))
                .ForMember(dest => dest.SubjectName,
                    opt => opt.MapFrom(src => src.Chapter.Subject.SubjectName))
                .ForMember(dest => dest.SubjectCode,
                    opt => opt.MapFrom(src => src.Chapter.Subject.SubjectCode))
                .ForMember(dest => dest.UploadedByName,
                    opt => opt.MapFrom(src => src.UploadedByUser.FullName))
                .ForMember(dest => dest.ChunkCount,
                    opt => opt.MapFrom(src => src.Chunks.Count))
                .ForMember(dest => dest.UploadedAt,
                    opt => opt.MapFrom(src => DateTimeHelper.ConvertToVietnamTime(src.UploadedAt)))
                .ForMember(dest => dest.IndexedAt,
                    opt => opt.MapFrom(src => DateTimeHelper.ConvertToVietnamTime(src.IndexedAt)));

            CreateMap<Document, DocumentDetailDto>()
                .IncludeBase<Document, DocumentDto>();

            // DocumentChunk
            CreateMap<DocumentChunk, ChunkDto>()
                .ForMember(dest => dest.HasEmbedding,
                    opt => opt.MapFrom(src => src.Embedding != null));

            // DocumentConflict
            CreateMap<DocumentConflict, DocumentConflictDto>()
                .ForMember(dest => dest.ConflictingDocumentTitle,
                    opt => opt.MapFrom(src => src.ConflictingDocument.Title));

            // PaymentTransaction
            CreateMap<PaymentTransaction, PaymentTransactionDto>()
                .ForMember(dest => dest.Username,
                    opt => opt.MapFrom(src => src.User.Username))
                .ForMember(dest => dest.UserFullName,
                    opt => opt.MapFrom(src => src.User.FullName));
        }
    }
}
