#pragma warning disable SKEXP0050
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Common.Constants;
using Microsoft.Extensions.Logging;
using System.Text;
using Microsoft.SemanticKernel.Text;

using Microsoft.Extensions.Configuration;

namespace LearningDocumentSystem.Business.Services.Implementations
{
    public class ChunkingService : IChunkingService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<ChunkingService> _logger;

        public ChunkingService(IConfiguration config, ILogger<ChunkingService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<List<(string Content, int PageNumber)>> ExtractChunksAsync(string filePath, string fileType)
        {
            _logger.LogInformation("Extracting chunks from {FileType} file: {Path}", fileType, filePath);

            var rawText = fileType.ToLowerInvariant() switch
            {
                "pdf"  => await ExtractPdfTextAsync(filePath),
                "docx" => await ExtractDocxTextAsync(filePath),
                "pptx" => await ExtractPptxTextAsync(filePath),
                _      => throw new NotSupportedException($"File type '{fileType}' không được hỗ trợ.")
            };

            return ChunkText(rawText);
        }

        // ============================================================
        // PDF - sử dụng iText7
        // ============================================================
        private Task<List<(string Text, int Page)>> ExtractPdfTextAsync(string filePath)
        {
            var result = new List<(string Text, int Page)>();
            try
            {
                using var reader = new PdfReader(filePath);
                using var pdf    = new PdfDocument(reader);
                for (int i = 1; i <= pdf.GetNumberOfPages(); i++)
                {
                    var text = PdfTextExtractor.GetTextFromPage(pdf.GetPage(i)).Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                        result.Add((text, i));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting PDF: {Path}", filePath);
            }
            return Task.FromResult(result);
        }

        // ============================================================
        // DOCX - sử dụng DocumentFormat.OpenXml
        // ============================================================
        private Task<List<(string Text, int Page)>> ExtractDocxTextAsync(string filePath)
        {
            var result = new List<(string Text, int Page)>();
            try
            {
                using var doc    = WordprocessingDocument.Open(filePath, false);
                var body         = doc.MainDocumentPart?.Document?.Body;
                if (body == null) return Task.FromResult(result);

                var sb = new StringBuilder();
                foreach (var para in body.Elements<Paragraph>())
                {
                    var text = para.InnerText.Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                        sb.AppendLine(text);
                }
                result.Add((sb.ToString(), 1));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting DOCX: {Path}", filePath);
            }
            return Task.FromResult(result);
        }

        // ============================================================
        // PPTX - sử dụng DocumentFormat.OpenXml
        // ============================================================
        private Task<List<(string Text, int Page)>> ExtractPptxTextAsync(string filePath)
        {
            var result = new List<(string Text, int Page)>();
            try
            {
                using var prs = PresentationDocument.Open(filePath, false);
                var slides    = prs.PresentationPart?.SlideParts?.ToList();
                if (slides == null) return Task.FromResult(result);

                for (int i = 0; i < slides.Count; i++)
                {
                    var sb = new StringBuilder();
                    var shapes = slides[i].Slide?.CommonSlideData?.ShapeTree
                        ?.Elements<DocumentFormat.OpenXml.Presentation.Shape>() ?? [];
                    foreach (var shape in shapes)
                    {
                        var txt = shape.TextBody?.InnerText?.Trim();
                        if (!string.IsNullOrWhiteSpace(txt))
                            sb.AppendLine(txt);
                    }
                    if (sb.Length > 0)
                        result.Add((sb.ToString(), i + 1));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting PPTX: {Path}", filePath);
            }
            return Task.FromResult(result);
        }

        private List<(string Content, int PageNumber)> ChunkText(List<(string Text, int Page)> pages)
        {
            _logger.LogInformation("Gom nhóm phân mảnh sử dụng Microsoft Semantic Kernel TextChunker.");
            var chunks = new List<(string Content, int PageNumber)>();

            // Định nghĩa custom TokenCounter để tính toán độ dài theo KÝ TỰ (Characters)
            TextChunker.TokenCounter characterCounter = input => input.Length;

            var chunkSize = _config.GetValue<int>("AppSettings:ChunkSize", 800);
            var chunkOverlap = _config.GetValue<int>("AppSettings:ChunkOverlap", 100);
            var minChunkLength = _config.GetValue<int>("AppSettings:MinChunkLength", 50);

            foreach (var (text, page) in pages)
            {
                if (string.IsNullOrWhiteSpace(text)) continue;

                // 1. Chia tách văn bản của trang hiện tại thành các câu/dòng nhỏ (tối đa 200 ký tự)
                // để tránh cắt nửa câu ở giữa một cách tùy tiện.
                var lines = TextChunker.SplitPlainTextLines(text, 200, characterCounter);

                // 2. Gom nhóm các câu/dòng trên thành các đoạn văn lớn (chunk)
                var paragraphs = TextChunker.SplitPlainTextParagraphs(lines, chunkSize, chunkOverlap, tokenCounter: characterCounter);

                foreach (var p in paragraphs)
                {
                    var trimmed = p.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed) && trimmed.Length >= minChunkLength)
                    {
                        chunks.Add((trimmed, page));
                    }
                }
            }

            // Nếu không trích xuất được bất kỳ nội dung nào → trả về 1 chunk placeholder
            if (chunks.Count == 0)
            {
                chunks.Add(("Tài liệu chưa có nội dung text hoặc định dạng không được hỗ trợ.", 1));
            }

            return chunks;
        }
    }
}
