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

        // ============================================================
        // PUBLIC: Overload 1 – dùng cài đặt từ appsettings (legacy)
        // ============================================================
        public async Task<List<(string Content, int PageNumber)>> ExtractChunksAsync(string filePath, string fileType)
        {
            var strategy      = _config.GetValue<string>("AppSettings:ChunkStrategy", "Recursive") ?? "Recursive";
            var chunkSize     = _config.GetValue<int>("AppSettings:ChunkSize", 800);
            var chunkOverlap  = _config.GetValue<int>("AppSettings:ChunkOverlap", 100);
            var minChunkLen   = _config.GetValue<int>("AppSettings:MinChunkLength", 50);

            return await ExtractChunksAsync(filePath, fileType, strategy, chunkSize, chunkOverlap, minChunkLen);
        }

        // ============================================================
        // PUBLIC: Overload 2 – dùng params do Giảng viên cấu hình
        // ============================================================
        public async Task<List<(string Content, int PageNumber)>> ExtractChunksAsync(
            string filePath, string fileType,
            string strategy, int chunkSize, int chunkOverlap, int minChunkLength)
        {
            _logger.LogInformation(
                "Extracting chunks: type={FileType}, strategy={Strategy}, size={Size}, overlap={Overlap}, minLen={Min}",
                fileType, strategy, chunkSize, chunkOverlap, minChunkLength);

            var rawText = fileType.ToLowerInvariant() switch
            {
                "pdf"  => await ExtractPdfTextAsync(filePath),
                "docx" => await ExtractDocxTextAsync(filePath),
                "pptx" => await ExtractPptxTextAsync(filePath),
                _      => throw new NotSupportedException($"File type '{fileType}' không được hỗ trợ.")
            };

            return strategy.ToLowerInvariant() switch
            {
                "fixedsize" => ChunkFixedSize(rawText, chunkSize, chunkOverlap, minChunkLength),
                "paragraph" => ChunkParagraph(rawText, chunkSize, minChunkLength),
                _           => ChunkRecursive(rawText, chunkSize, chunkOverlap, minChunkLength) // "recursive" + default
            };
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
                using var doc = WordprocessingDocument.Open(filePath, false);
                var body      = doc.MainDocumentPart?.Document?.Body;
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

        // ============================================================
        // STRATEGY 1: Fixed-Size Chunking
        // Chia văn bản thành các đoạn kích thước cố định theo số ký tự.
        // ============================================================
        private List<(string Content, int PageNumber)> ChunkFixedSize(
            List<(string Text, int Page)> pages,
            int chunkSize, int chunkOverlap, int minChunkLength)
        {
            _logger.LogInformation("Strategy: Fixed-Size (size={Size}, overlap={Overlap})", chunkSize, chunkOverlap);
            var chunks = new List<(string Content, int PageNumber)>();

            foreach (var (text, page) in pages)
            {
                if (string.IsNullOrWhiteSpace(text)) continue;

                int start = 0;
                while (start < text.Length)
                {
                    int end = Math.Min(start + chunkSize, text.Length);
                    var chunk = text[start..end].Trim();

                    if (!string.IsNullOrWhiteSpace(chunk) && chunk.Length >= minChunkLength)
                        chunks.Add((chunk, page));

                    // Di chuyển với overlap: bước = chunkSize - overlap
                    int step = chunkSize - chunkOverlap;
                    if (step <= 0) step = chunkSize; // tránh vòng lặp vô hạn
                    start += step;
                }
            }

            return EnsureNotEmpty(chunks);
        }

        // ============================================================
        // STRATEGY 2: Paragraph-based Chunking
        // Chia theo đoạn văn (\n\n), gộp sao cho không vượt quá maxSize.
        // ============================================================
        private List<(string Content, int PageNumber)> ChunkParagraph(
            List<(string Text, int Page)> pages,
            int maxSize, int minChunkLength)
        {
            _logger.LogInformation("Strategy: Paragraph (maxSize={MaxSize})", maxSize);
            var chunks = new List<(string Content, int PageNumber)>();

            foreach (var (text, page) in pages)
            {
                if (string.IsNullOrWhiteSpace(text)) continue;

                // Tách theo double newline (đoạn văn)
                var paragraphs = text
                    .Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToList();

                foreach (var para in paragraphs)
                {
                    if (para.Length <= maxSize)
                    {
                        if (para.Length >= minChunkLength)
                            chunks.Add((para, page));
                        continue;
                    }

                    // Nếu đoạn văn dài hơn maxSize -> Chia theo câu
                    var sentences = System.Text.RegularExpressions.Regex.Split(para, @"(?<=[.!?])\s+")
                        .Select(s => s.Trim())
                        .Where(s => s.Length > 0)
                        .ToList();

                    var sentenceBuffer = new StringBuilder();

                    foreach (var sentence in sentences)
                    {
                        if (sentence.Length > maxSize)
                        {
                            // Flush current sentenceBuffer if any
                            if (sentenceBuffer.Length >= minChunkLength)
                                chunks.Add((sentenceBuffer.ToString().Trim(), page));
                            sentenceBuffer.Clear();

                            // Chia câu dài này theo từ
                            var words = sentence.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            var wordBuffer = new StringBuilder();

                            foreach (var word in words)
                            {
                                if (word.Length > maxSize)
                                {
                                    // Flush current wordBuffer if any
                                    if (wordBuffer.Length >= minChunkLength)
                                        chunks.Add((wordBuffer.ToString().Trim(), page));
                                    wordBuffer.Clear();

                                    // Cắt cứng từ quá dài theo ký tự
                                    int sWord = 0;
                                    while (sWord < word.Length)
                                    {
                                        int eWord = Math.Min(sWord + maxSize, word.Length);
                                        chunks.Add((word[sWord..eWord], page));
                                        sWord += maxSize;
                                    }
                                }
                                else
                                {
                                    if (wordBuffer.Length + word.Length + (wordBuffer.Length > 0 ? 1 : 0) > maxSize)
                                    {
                                        var flushedWord = wordBuffer.ToString().Trim();
                                        if (flushedWord.Length >= minChunkLength)
                                            chunks.Add((flushedWord, page));
                                        wordBuffer.Clear();
                                    }
                                    if (wordBuffer.Length > 0) wordBuffer.Append(' ');
                                    wordBuffer.Append(word);
                                }
                            }

                            // Flush remaining wordBuffer
                            if (wordBuffer.Length > 0)
                            {
                                var remainingWord = wordBuffer.ToString().Trim();
                                if (remainingWord.Length >= minChunkLength)
                                    chunks.Add((remainingWord, page));
                            }
                        }
                        else
                        {
                            if (sentenceBuffer.Length + sentence.Length + (sentenceBuffer.Length > 0 ? 1 : 0) > maxSize)
                            {
                                var flushedSentence = sentenceBuffer.ToString().Trim();
                                if (flushedSentence.Length >= minChunkLength)
                                    chunks.Add((flushedSentence, page));
                                sentenceBuffer.Clear();
                            }
                            if (sentenceBuffer.Length > 0) sentenceBuffer.Append(' ');
                            sentenceBuffer.Append(sentence);
                        }
                    }

                    // Flush remaining sentenceBuffer
                    if (sentenceBuffer.Length > 0)
                    {
                        var remainingSentence = sentenceBuffer.ToString().Trim();
                        if (remainingSentence.Length >= minChunkLength)
                            chunks.Add((remainingSentence, page));
                    }
                }
            }

            return EnsureNotEmpty(chunks);
        }

        // ============================================================
        // STRATEGY 3: Recursive Character Chunking (hiện tại)
        // Dùng Microsoft.SemanticKernel.Text.TextChunker theo thứ tự ưu tiên:
        // đoạn văn → dòng → khoảng trắng → ký tự
        // ============================================================
        private List<(string Content, int PageNumber)> ChunkRecursive(
            List<(string Text, int Page)> pages,
            int chunkSize, int chunkOverlap, int minChunkLength)
        {
            _logger.LogInformation("Strategy: Recursive (size={Size}, overlap={Overlap})", chunkSize, chunkOverlap);
            var chunks = new List<(string Content, int PageNumber)>();

            // Định nghĩa custom TokenCounter theo KÝ TỰ
            TextChunker.TokenCounter characterCounter = input => input.Length;

            foreach (var (text, page) in pages)
            {
                if (string.IsNullOrWhiteSpace(text)) continue;

                // 1. Chia tách thành các câu/dòng nhỏ (tối đa 200 ký tự)
                var lines = TextChunker.SplitPlainTextLines(text, 200, characterCounter);

                // 2. Gom nhóm thành các đoạn lớn (chunk)
                var paragraphs = TextChunker.SplitPlainTextParagraphs(lines, chunkSize, chunkOverlap, tokenCounter: characterCounter);

                foreach (var p in paragraphs)
                {
                    var trimmed = p.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed) && trimmed.Length >= minChunkLength)
                        chunks.Add((trimmed, page));
                }
            }

            return EnsureNotEmpty(chunks);
        }

        // ============================================================
        // HELPER: Đảm bảo luôn trả về ít nhất 1 chunk placeholder
        // ============================================================
        private static List<(string Content, int PageNumber)> EnsureNotEmpty(List<(string Content, int PageNumber)> chunks)
        {
            if (chunks.Count == 0)
                chunks.Add(("Tài liệu chưa có nội dung text hoặc định dạng không được hỗ trợ.", 1));
            return chunks;
        }
    }
}
