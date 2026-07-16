# 📚 Hệ Thống Quản Lý Tài Liệu Học Tập (Learning Document System - LDS)

Chào mừng bạn đến với **Learning Document System (LDS)**, giải pháp quản lý tài liệu thông minh kết hợp trợ lý ảo AI hỗ trợ học tập theo thời gian thực. Hệ thống được xây dựng trên nền tảng **ASP.NET Core (Razor Pages)** và ứng dụng kỹ thuật **RAG (Retrieval-Augmented Generation)** để phân tích, tìm kiếm thông tin ngữ nghĩa và trả lời câu hỏi trực tiếp dựa trên tài liệu bài giảng.

> ⚠️ **Lưu ý bảo mật**: Không commit `appsettings.json` chứa connection string, API key hoặc VNPay secret thật lên GitHub. README này chỉ dùng placeholder để hướng dẫn cấu hình.

---

## ✨ Chức Năng Chính

- **Xác thực & Phân quyền**: Đăng ký, đăng nhập bằng Cookie Authentication. Phân quyền người dùng (`Admin`, `Teacher`, `Student`).
- **Gói Dịch Vụ (Subscription)**: Hỗ trợ các gói `Free`, `Plus`, `Pro` với giới hạn số lượng tin nhắn khác nhau.
- **Thanh Toán**: Tích hợp VNPay sandbox cho phép nâng cấp gói dịch vụ.
- **Quản Trị (Admin)**: Quản lý người dùng, email được phép đăng ký, cấu hình chunking tài liệu, gói dịch vụ và benchmark AI.
- **Giảng Viên (Teacher)**: Quản lý môn học, chương học, upload tài liệu giảng dạy (`.pdf`, `.docx`, `.pptx`).
- **Xử Lý Tài Liệu (RAG)**: Hệ thống tự động tách tài liệu thành chunks, sinh vector embedding nội bộ và lưu nguồn tham chiếu.
- **Trợ Lý Ảo (Student)**: Học sinh chat hỏi đáp theo tài liệu học tập bằng RAG. Hỗ trợ đa dạng provider AI: **Gemini**, **Groq** và **OpenAI-compatible**.
- **Real-time**: Sử dụng SignalR đẩy thông báo cập nhật thời gian thực khi tài liệu, dashboard hoặc chat thay đổi.

---

## 🗺️ Sơ Đồ Kiến Trúc Hệ Thống (Architecture)

Dự án được thiết kế theo mô hình kiến trúc **3 lớp (3-Tier Architecture)** phân tách rõ ràng trách nhiệm để dễ bảo trì và nâng cấp:

<img width="1271" height="692" alt="GroupProject" src="https://github.com/HoangJunior2005/GroupProject/blob/fix/readMe/GroupProject.jpg" />

### 1. Tầng Dữ Liệu (LearningDocumentSystem.Data)
*   **Entities**: Định nghĩa cấu trúc các bảng trong cơ sở dữ liệu.
*   **AppDbContext**: Cấu hình các mối quan hệ, ràng buộc dữ liệu (Entity Framework Core 8).
*   **Repositories & Unit of Work**: Đóng gói các tác vụ truy xuất dữ liệu.
*   **Migrations**: Lưu vết các thay đổi cấu trúc Database.
*   **Seeders (DataSeeder)**: Tự động khởi tạo dữ liệu mẫu khi ứng dụng khởi chạy lần đầu.

### 2. Tầng Nghiệp Vụ (LearningDocumentSystem.Business)
*   **Services**: Xử lý logic nghiệp vụ chính (Auth, Quản lý môn học/chương học, Upload, Thanh toán VNPay).
*   **Document Chunking**: Trích xuất văn bản từ tài liệu và chia nhỏ theo chiến lược (Recursive, Fixed Size...).
*   **Embedding & Vectorization**: Tự sinh các vector embedding nội bộ từ các phân đoạn tài liệu để so khớp độ tương đồng ngữ nghĩa.
*   **AI Integration**: Tích hợp API Gemini, Groq, và OpenAI để trả lời câu hỏi dựa trên các phân đoạn tài liệu phù hợp nhất được tìm thấy (quy trình **RAG**).
*   **AutoMapper**: Cấu hình ánh xạ tự động giữa Entities và DTOs.

### 3. Tầng Giao Diện (LearningDocumentSystem.Web)
*   **Razor Pages**: Giao diện người dùng động.
*   **ViewModels**: Chuẩn hóa dữ liệu đầu vào và đầu ra cho View.
*   **SignalR Hubs**: Đẩy thông báo thời gian thực đến người dùng.
*   **Cookie Authentication**: Cơ chế bảo mật và phân quyền vai trò.

---

## 📁 Cấu Trúc Thư Mục Dự Án (Repository Structure)

Cấu trúc cây thư mục tổ chức mã nguồn của hệ thống:

```text
GroupProject/
├── GroupProject.jpg                  # File sơ đồ kiến trúc hệ thống
├── README.md                        # Tài liệu hướng dẫn sử dụng
└── LearningDocumentSystem/          # Thư mục chính chứa mã nguồn
    ├── LearningDocumentSystem.slnx
    ├── LearningDocumentSystem.Data/ # Tầng dữ liệu (DAL)
    │   ├── DbContexts/
    │   ├── Entities/
    │   ├── Migrations/
    │   ├── Repositories/
    │   └── Seeders/
    ├── LearningDocumentSystem.Business/ # Tầng nghiệp vụ (BLL)
    │   ├── DTOs/
    │   ├── Mapping/
    │   └── Services/
    └── LearningDocumentSystem.Web/  # Tầng giao diện (PL)
        ├── Pages/
        ├── Hubs/
        ├── Services/
        ├── ViewModels/
        ├── wwwroot/                 # CSS, JS, thư viện giao diện, thư mục uploads
        ├── App_Data/
        │   └── package-plans.json   # Cấu hình các gói dịch vụ mặc định
        ├── appsettings.json         # Tệp cấu hình môi trường ứng dụng
        └── Program.cs               # Điểm khởi chạy, thiết lập DI, Auth, Middleware
```

---

## 💾 Cấu Hình Cơ Sở Dữ Liệu & Ứng Dụng

Hệ thống sử dụng **Microsoft SQL Server** làm hệ quản trị cơ sở dữ liệu.

### 1. Hướng dẫn cấu hình `appsettings.json`

Tạo hoặc cập nhật tệp cấu hình tại đường dẫn `LearningDocumentSystem/LearningDocumentSystem.Web/appsettings.json` như sau:

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

> [!IMPORTANT]
> Nếu dùng **Windows Authentication** cho SQL Server (không dùng tài khoản `sa`), hãy đổi chuỗi kết nối thành:
> `"DefaultConnection": "Server=localhost;Database=LearningDocumentSystemDataBase;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"`

### 2. Tự động Khởi tạo Cơ sở dữ liệu (Database Seeding)

Khi bạn chạy dự án lần đầu, hệ thống sẽ **tự động chạy Migrations** để tạo cơ sở dữ liệu và thực thi `DataSeeder` để nạp sẵn dữ liệu mẫu.

**Danh sách tài khoản mẫu để bạn đăng nhập thử nghiệm:**

| Vai Trò | Username | Email | Mật Khẩu |
| --- | --- | --- | --- |
| **Admin** | `admin` | `admin@university.edu.vn` | `Admin@123` |
| **Teacher** | `nguyenvan_gv` | `teacher@university.edu.vn` | `Teacher@123` |
| **Student** | `tranmanh_sv` | `student@student.edu.vn` | `Student@123` |

---

## 🚀 Hướng Dẫn Chạy Dự Án (How to Run)

### Điều kiện cần
1. **.NET 8 SDK**.
2. **SQL Server** (Bản Developer/Express) hoặc LocalDB.
3. API key cho provider AI (Gemini từ Google AI Studio, Groq Console, hoặc OpenAI).

### Cách 1: Chạy bằng Visual Studio 2022
1. Mở file `LearningDocumentSystem/LearningDocumentSystem.slnx`.
2. Chọn startup project là `LearningDocumentSystem.Web`.
3. Kiểm tra lại `appsettings.json`.
4. Bấm **F5** hoặc chọn profile `https` để chạy.

### Cách 2: Chạy bằng Dòng lệnh (CLI)
Mở terminal tại thư mục gốc repository và chạy:

```bash
# Phục hồi các gói nuget
dotnet restore LearningDocumentSystem/LearningDocumentSystem.slnx

# Chạy dự án
dotnet run --project LearningDocumentSystem/LearningDocumentSystem.Web/LearningDocumentSystem.Web.csproj
```
Ứng dụng sẽ chạy tại `http://localhost:5107` hoặc `https://localhost:7059`.

---

## 🎯 Hướng Dẫn Sử Dụng Nhanh

### Đối với Admin
1. Quản lý user, duyệt email được phép đăng ký và cấu hình các gói dịch vụ (Packages).
2. Cấu hình chunking (Chiến lược, chunk size, overlap) trực tiếp trên giao diện.
3. Theo dõi hệ thống qua Dashboard và thực hiện Benchmark AI.

### Đối với Teacher (Giảng Viên)
1. Tạo môn học và chương học.
2. Upload tài liệu giảng dạy. Hệ thống tự động lưu vào `wwwroot/uploads`, tách nội dung, tạo embedding và chuyển trạng thái sang `Indexed`.
3. Quản lý (xem, tải xuống, xóa) các tài liệu đã tải lên.

### Đối với Student (Học Sinh)
1. Chọn môn học hoặc chương học muốn học.
2. Đặt câu hỏi trong cửa sổ Chat.
3. Hệ thống tìm kiếm theo ngữ nghĩa và AI sẽ tổng hợp câu trả lời, đồng thời trích xuất nguồn tài liệu tham chiếu.
4. Có thể nâng cấp gói dịch vụ (`Plus`, `Pro`) qua VNPay để mở rộng giới hạn tin nhắn/ngày và dùng nhiều provider AI hơn.

### Gói Dịch Vụ Mặc Định
| Gói | Giá (VND) | Giới hạn/ngày | Hỗ trợ Provider AI |
| --- | ---: | --- | --- |
| **Free** | 0 | 20 tin nhắn | Gemini |
| **Plus** | 99,000 | 100 tin nhắn | Gemini, Groq |
| **Pro** | 199,000 | Không giới hạn | Gemini, Groq, OpenAI |

---

## ⚙️ Các Lệnh Thường Dùng (Cho Nhóm Phát Triển)

**Tạo migration mới:**
```bash
dotnet ef migrations add MigrationName --project LearningDocumentSystem/LearningDocumentSystem.Data --startup-project LearningDocumentSystem/LearningDocumentSystem.Web
```

**Cập nhật database thủ công:**
```bash
dotnet ef database update --project LearningDocumentSystem/LearningDocumentSystem.Data --startup-project LearningDocumentSystem/LearningDocumentSystem.Web
```

**Troubleshooting:**
- **Không kết nối được Database**: Kiểm tra SQL Server, Database Name, quyền của tài khoản.
- **Lỗi Upload**: Kiểm tra định dạng `AllowedFileTypes`, dung lượng `MaxFileSizeMB`, và quyền ghi thư mục `wwwroot/uploads`.
- **Lỗi AI Provider**: Đảm bảo API Key hợp lệ và Model Name chính xác trong `appsettings.json`.
- **Thanh toán VNPay lỗi**: Kiểm tra thông tin Sandbox (`TmnCode`, `HashSecret`, `PaymentUrl`).
