# Learning Document System (LDS)

Learning Document System là hệ thống quản lý tài liệu học tập xây dựng bằng ASP.NET Core Razor Pages. Ứng dụng hỗ trợ quản lý môn học, chương học, tài liệu giảng dạy, chat AI theo tài liệu đã upload, phân quyền người dùng và thanh toán gói sử dụng qua VNPay sandbox.

> Lưu ý bảo mật: không commit `appsettings.json` chứa connection string, API key hoặc VNPay secret thật lên GitHub. README này chỉ dùng placeholder để hướng dẫn cấu hình.

## Chức năng chính

- Đăng ký, đăng nhập bằng Cookie Authentication.
- Phân quyền `Admin`, `Teacher`, `Student`, kèm các gói `Free`, `Plus`, `Pro`.
- Admin quản lý người dùng, email được phép đăng ký, cấu hình chunking, gói dịch vụ và benchmark AI.
- Teacher quản lý môn học, chương học, upload tài liệu `.pdf`, `.docx`, `.pptx`.
- Hệ thống tách tài liệu thành chunks, sinh embedding nội bộ và lưu nguồn tham chiếu.
- Student chat hỏi đáp theo tài liệu học tập bằng RAG.
- Hỗ trợ nhiều provider AI: Gemini, Groq và OpenAI-compatible endpoint.
- SignalR đẩy cập nhật thời gian thực khi tài liệu, dashboard hoặc chat thay đổi.
- VNPay sandbox cho thanh toán nâng cấp gói.

## Công nghệ sử dụng

- .NET 8
- ASP.NET Core Razor Pages
- Entity Framework Core 8
- SQL Server
- SignalR
- AutoMapper
- iText 7, OpenXML
- Bootstrap, jQuery
- Gemini / Groq / OpenAI-compatible APIs

## Cấu trúc source code

```text
FinalPRN222/
├── ASS2.drawio.png
├── README.md
└── LearningDocumentSystem/
    ├── LearningDocumentSystem.slnx
    ├── LearningDocumentSystem.Web/
    │   ├── Pages/
    │   ├── Hubs/
    │   ├── Services/
    │   ├── ViewModels/
    │   ├── wwwroot/
    │   ├── App_Data/package-plans.json
    │   └── Program.cs
    ├── LearningDocumentSystem.Business/
    │   ├── DTOs/
    │   ├── Mapping/
    │   └── Services/
    └── LearningDocumentSystem.Data/
        ├── DbContexts/
        ├── Entities/
        ├── Migrations/
        ├── Repositories/
        └── Seeders/
```

### Vai trò từng project

`LearningDocumentSystem.Web` là tầng giao diện Razor Pages, cấu hình middleware, authentication, SignalR hub và dependency injection.

`LearningDocumentSystem.Business` chứa logic nghiệp vụ như auth, document upload, chunking, embedding, chat RAG, package, benchmark và VNPay.

`LearningDocumentSystem.Data` chứa entity, DbContext, migration, repository, unit of work, helper và seeder dữ liệu mẫu.

## Yêu cầu trước khi chạy

1. Cài .NET 8 SDK.
2. Cài SQL Server Developer/Express hoặc dùng SQL Server local có sẵn.
3. Cài Visual Studio 2022 hoặc Visual Studio Code.
4. Chuẩn bị API key cho provider AI muốn dùng:
   - Gemini: Google AI Studio.
   - Groq: Groq Console.
   - OpenAI-compatible: endpoint tương thích `/v1/chat/completions`.
5. Nếu test thanh toán, chuẩn bị thông tin VNPay sandbox.

## Cấu hình `appsettings.json`

Tạo hoặc cập nhật file:

```text
LearningDocumentSystem/LearningDocumentSystem.Web/appsettings.json
```

Mẫu cấu hình:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LearningDocumentSystemDataBase;user id=sa;password=YOUR_SQL_PASSWORD;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "AppSettings": {
    "UploadFolder": "uploads",
    "MaxFileSizeMB": 50,
    "AllowedFileTypes": [ "pdf", "docx", "pptx" ],
    "ChunkStrategy": "Recursive",
    "ChunkSize": 800,
    "ChunkOverlap": 100,
    "MinChunkLength": 50,
    "DefaultPageSize": 10
  },
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY",
    "ModelName": "gemini-2.5-flash"
  },
  "OpenAI": {
    "ApiKey": "YOUR_OPENAI_COMPATIBLE_API_KEY",
    "ModelName": "gpt-oss-120b",
    "BaseUrl": "https://api.example.com/v1/chat/completions"
  },
  "Groq": {
    "ApiKey": "YOUR_GROQ_API_KEY",
    "ModelName": "llama-3.1-8b-instant"
  },
  "Vnpay": {
    "TmnCode": "YOUR_VNPAY_TMN_CODE",
    "HashSecret": "YOUR_VNPAY_HASH_SECRET",
    "PaymentUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
    "Version": "2.1.0",
    "Command": "pay"
  }
}
```

Nếu dùng Windows Authentication cho SQL Server, đổi connection string thành:

```json
"DefaultConnection": "Server=localhost;Database=LearningDocumentSystemDataBase;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

## Chạy dự án

### Cách 1: chạy bằng CLI

Mở terminal tại thư mục gốc repository và chạy:

```bash
dotnet restore LearningDocumentSystem/LearningDocumentSystem.slnx
dotnet run --project LearningDocumentSystem/LearningDocumentSystem.Web/LearningDocumentSystem.Web.csproj
```

Ứng dụng mặc định chạy tại:

- HTTP: `http://localhost:5107`
- HTTPS: `https://localhost:7059`

### Cách 2: chạy bằng Visual Studio

1. Mở `LearningDocumentSystem/LearningDocumentSystem.slnx`.
2. Chọn startup project là `LearningDocumentSystem.Web`.
3. Kiểm tra lại `appsettings.json`.
4. Bấm F5 hoặc chọn profile `https`.

## Database và dữ liệu mẫu

Khi ứng dụng khởi động, `DataSeeder` sẽ tự chạy:

- Apply EF Core migrations vào SQL Server.
- Tạo roles: `Admin`, `Teacher`, `Student`, `Plus`, `Pro`.
- Tạo user mẫu, môn học, chương học, tài liệu demo, lịch sử chat, package plan và payment transaction mẫu.

Tài khoản đăng nhập mẫu:

| Vai trò | Username | Email | Mật khẩu |
| --- | --- | --- | --- |
| Admin | `admin` | `admin@university.edu.vn` | `Admin@123` |
| Teacher | `nguyenvan_gv` | `teacher@university.edu.vn` | `Teacher@123` |
| Student | `tranmanh_sv` | `student@student.edu.vn` | `Student@123` |

## Hướng dẫn sử dụng nhanh

### Admin

1. Đăng nhập bằng tài khoản admin.
2. Vào khu vực quản trị để quản lý user, email được phép đăng ký và gói dịch vụ.
3. Cấu hình chunking tại trang admin nếu muốn đổi strategy, chunk size, overlap hoặc độ dài tối thiểu.
4. Theo dõi benchmark AI và dashboard hệ thống.

### Teacher

1. Đăng nhập bằng tài khoản teacher.
2. Tạo môn học và chương học.
3. Upload tài liệu vào chương tương ứng.
4. Hệ thống lưu file vào `wwwroot/uploads`, tách nội dung, tạo embedding và chuyển trạng thái tài liệu sang `Indexed`.
5. Có thể xem danh sách, chi tiết, download hoặc xóa tài liệu đã upload.

### Student

1. Đăng nhập bằng tài khoản student.
2. Vào trang chat.
3. Chọn môn học hoặc chương học nếu muốn giới hạn phạm vi hỏi đáp.
4. Đặt câu hỏi dựa trên tài liệu đã được index.
5. Xem câu trả lời AI và nguồn tài liệu tham chiếu nếu hệ thống tìm thấy nội dung phù hợp.

### Gói dịch vụ

Các gói mặc định được seed từ code và file `App_Data/package-plans.json`:

| Gói | Giá | Giới hạn/ngày | Provider |
| --- | ---: | --- | --- |
| Free | 0 VND | 20 tin nhắn | Gemini |
| Plus | 99,000 VND | 100 tin nhắn | Gemini, Groq |
| Pro | 199,000 VND | Không giới hạn | Gemini, Groq, OpenAI |

## Luồng xử lý tài liệu và chat

1. Teacher upload tài liệu.
2. `DocumentService` kiểm tra trùng title, tên file và hash nội dung.
3. File được lưu vào thư mục upload.
4. `ChunkingService` trích xuất nội dung theo cấu hình chunking.
5. `EmbeddingService` sinh vector embedding cho từng chunk.
6. Khi student hỏi, `ChatService` sinh embedding cho câu hỏi.
7. Hệ thống tìm các chunk liên quan theo semantic score và keyword boost.
8. Provider AI được chọn tạo câu trả lời dựa trên context tìm được.
9. Chat session, message, token, latency, feedback và nguồn tham chiếu được lưu lại.

## Lệnh thường dùng

Restore package:

```bash
dotnet restore LearningDocumentSystem/LearningDocumentSystem.slnx
```

Build solution:

```bash
dotnet build LearningDocumentSystem/LearningDocumentSystem.slnx
```

Chạy web app:

```bash
dotnet run --project LearningDocumentSystem/LearningDocumentSystem.Web/LearningDocumentSystem.Web.csproj
```

Tạo migration mới:

```bash
dotnet ef migrations add MigrationName \
  --project LearningDocumentSystem/LearningDocumentSystem.Data \
  --startup-project LearningDocumentSystem/LearningDocumentSystem.Web
```

Update database thủ công:

```bash
dotnet ef database update \
  --project LearningDocumentSystem/LearningDocumentSystem.Data \
  --startup-project LearningDocumentSystem/LearningDocumentSystem.Web
```

## Troubleshooting

Nếu không kết nối được database, kiểm tra SQL Server đang chạy, database name đúng và tài khoản trong connection string có quyền tạo database.

Nếu upload file bị lỗi, kiểm tra định dạng file có nằm trong `AllowedFileTypes`, dung lượng không vượt `MaxFileSizeMB`, và thư mục `wwwroot/uploads` có quyền ghi.

Nếu chat không trả lời đúng tài liệu, kiểm tra tài liệu đã ở trạng thái `Indexed`, đã có chunk/embedding, và bạn đã chọn đúng môn học hoặc chương học.

Nếu provider AI báo lỗi, kiểm tra API key, model name và base URL trong `appsettings.json`.

Nếu thanh toán VNPay không hoạt động, kiểm tra `TmnCode`, `HashSecret`, `PaymentUrl` sandbox và return URL của ứng dụng.

## Ghi chú cho nhóm phát triển

- Không commit secret thật.
- Không commit file upload sinh ra trong quá trình test.
- Khi sửa entity, tạo migration trong project `LearningDocumentSystem.Data`.
- Khi thêm service mới, đăng ký DI trong `LearningDocumentSystem.Web/Program.cs`.
- Khi đổi logic upload/chunking/chat, nên kiểm tra lại luồng Teacher upload và Student chat end-to-end.
