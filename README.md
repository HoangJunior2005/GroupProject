# 📚 Hệ Thống Quản Lý Tài Liệu Học Tập (Learning Document System - LDS)

Chào mừng bạn đến với **Learning Document System (LDS)**, giải pháp quản lý tài liệu thông minh kết hợp trợ lý ảo AI hỗ trợ học tập theo thời gian thực. Hệ thống được xây dựng trên nền tảng **ASP.NET Core (Razor Pages)** và ứng dụng kỹ thuật **RAG (Retrieval-Augmented Generation)** để phân tích, tìm kiếm thông tin ngữ nghĩa và trả lời câu hỏi trực tiếp dựa trên tài liệu bài giảng.

---

## 🗺️ Sơ Đồ Kiến Trúc Hệ Thống (Architecture)

Dự án được thiết kế theo mô hình kiến trúc **3 lớp (3-Tier Architecture)** phân tách rõ ràng trách nhiệm để dễ bảo trì và nâng cấp:

```mermaid
graph TD
    %% Tầng Presentation (Web)
    subgraph Presentation_Layer [Tầng Giao Diện - Web Project]
        A[Razor Pages UI] <--> B[ViewModels / DTOs]
        C[SignalR Notification Hub] <--> A
        D[Static Files CSS/JS] --> A
    end

    %% Tầng Business (Business Logic)
    subgraph Business_Layer [Tầng Nghiệp Vụ - Business Project]
        E[Auth Service]
        F[Document & Chunking Service]
        G[Embedding & Chat Service]
        H[Gemini AI Service]
        
        %% Mối quan hệ trong Business
        F -->|Băm từ tài liệu PDF/Docx| G
        G -->|Tìm kiếm tương đồng ngữ nghĩa| H
    end

    %% Tầng Data (Data Access)
    subgraph Data_Layer [Tầng Dữ Liệu - Data Project]
        I[AppDbContext EF Core]
        J[Generic Repositories / Unit of Work]
        K[SQL Server Database]
        L[Data Seeder]
        
        I <--> J
        J <--> K
        L -->|Khởi tạo| I
    end

    %% Liên kết giữa các Tầng
    Presentation_Layer <-->|Giao tiếp Service| Business_Layer
    Business_Layer <-->|Truy vấn dữ liệu| Data_Layer
```

### 1. Tầng Dữ Liệu (LearningDocumentSystem.Data)
*   **Entities**: Định nghĩa cấu trúc các bảng trong cơ sở dữ liệu.
*   **AppDbContext**: Cấu hình các mối quan hệ (1-1, 1-n, n-n), ràng buộc dữ liệu nâng cao (Composite PK, Cascade Delete, Index tối ưu hóa) bằng Fluent API.
*   **Repositories & Unit of Work**: Đóng gói các tác vụ truy xuất dữ liệu giúp giảm sự phụ thuộc trực tiếp vào DbContext.
*   **Migrations**: Lưu vết các thay đổi cấu trúc Database.
*   **Seeders (DataSeeder)**: Tự động khởi tạo dữ liệu mẫu khi ứng dụng khởi chạy lần đầu.

### 2. Tầng Nghiệp Vụ (LearningDocumentSystem.Business)
*   **Services**: Xử lý logic nghiệp vụ chính (Đăng nhập/đăng ký, quản lý môn học, chương học, upload file).
*   **Document Chunking**: Trích xuất văn bản từ tài liệu (`.pdf`, `.docx`, `.pptx`) và chia nhỏ thành các phân đoạn (`DocumentChunk`) có kèm số trang.
*   **Feature Hashing & TF-IDF Vectorization**: Tự sinh các vector embedding nội bộ từ các phân đoạn tài liệu nhằm mục đích so khớp độ tương đồng ngữ nghĩa bằng thuật toán Cosine Similarity.
*   **Gemini AI Integration**: Tích hợp API Gemini để trả lời câu hỏi dựa trên các phân đoạn tài liệu phù hợp nhất được tìm thấy (quy trình **RAG**).

### 3. Tầng Giao Diện (LearningDocumentSystem.Web)
*   **Razor Pages**: Giao diện người dùng động.
*   **ViewModels**: Chuẩn hóa dữ liệu đầu vào và đầu ra cho View.
*   **SignalR Hubs (NotificationHub)**: Đẩy thông báo thời gian thực đến người dùng (ví dụ: thông báo khi tài liệu được băm/nhúng vector thành công hoặc phát hiện xung đột).
*   **Cookie Authentication**: Cơ chế bảo mật và phân quyền vai trò (Admin, Teacher, Student) bằng Cookie.

💡 *Bạn cũng có thể xem sơ đồ kiến trúc chi tiết được vẽ sẵn trong tệp [ASS2.drawio.png](file:///c:/Users/Administrator/Desktop/Assignment_2/ASS2.drawio.png) tại thư mục gốc.*

---

## 📁 Cấu Trúc Thư Mục Dự Án (Repository Structure)

Cấu trúc cây thư mục tổ chức mã nguồn của hệ thống như sau:

```text
Assignment_2/
├── .github/                         # Cấu hình GitHub (Workflow, CI/CD...)
├── LearningDocumentSystem/          # Thư mục chính chứa mã nguồn dự án
│   ├── LearningDocumentSystem.Data/ # Tầng dữ liệu (Data Access Layer - DAL)
│   │   ├── DbContexts/              # Quản lý DbContext kết nối cơ sở dữ liệu
│   │   ├── Entities/                # Chứa các Model ánh xạ trực tiếp vào các bảng database
│   │   ├── Repositories/            # Giao diện và triển khai mẫu thiết kế Repository / Unit of Work
│   │   ├── Migrations/              # Quản lý lịch sử và các bản nâng cấp cấu trúc Database
│   │   ├── Seeders/                 # Chứa mã nguồn tự động khởi tạo dữ liệu mẫu (DataSeeder)
│   │   ├── Constants/               # Chứa các hằng số dùng chung của hệ thống (AppConstants, AppMessages)
│   │   ├── Helpers/                 # Chứa các lớp tiện ích bổ trợ (Mã hóa mật khẩu, xử lý File, DateTime)
│   │   └── Exceptions/              # Định nghĩa các Exception tùy chỉnh dùng để bắt lỗi hệ thống
│   ├── LearningDocumentSystem.Business/ # Tầng nghiệp vụ (Business Logic Layer - BLL)
│   │   ├── Services/                # Triển khai các xử lý logic (Auth, Chat, Document, Embedding, Gemini AI...)
│   │   ├── DTOs/                    # Data Transfer Objects truyền dữ liệu sạch giữa các tầng
│   │   └── Mapping/                 # Cấu hình ánh xạ tự động AutoMapper (giữa Entities và DTOs)
│   └── LearningDocumentSystem.Web/  # Tầng giao diện (Presentation Layer - PL)
│       ├── Pages/                   # Các trang giao diện Razor Pages & logic Code-behind
│       ├── ViewModels/              # Chứa các mô hình dữ liệu đầu vào/ra phục vụ trực tiếp cho hiển thị UI
│       ├── Hubs/                    # SignalR Hubs dùng cho truyền thông thời gian thực (Real-time)
│       ├── Services/                # Chứa các Service bổ trợ riêng cho giao diện (Notification)
│       ├── wwwroot/                 # Chứa các tệp tĩnh công khai (CSS, JS, thư viện giao diện, file tải lên)
│       ├── appsettings.json         # Tệp cấu hình môi trường ứng dụng (Connection String, Gemini API Key)
│       └── Program.cs               # Điểm khởi chạy của ứng dụng, thiết lập DI, Auth và Middleware
├── ASS2.drawio.png                  # File sơ đồ kiến trúc hệ thống (dưới dạng ảnh chụp)
├── .gitignore                       # Danh sách các tệp tin/thư mục Git bỏ qua không theo dõi
└── README.md                        # Tài liệu hướng dẫn sử dụng và mô tả dự án này
```

---

## 💾 Cấu Hình Cơ Sở Dữ Liệu (Database Configuration)

Hệ thống sử dụng **Microsoft SQL Server** làm hệ quản trị cơ sở dữ liệu chính.

### 1. Sơ đồ các bảng dữ liệu
Dưới đây là các bảng chính trong Database:
*   `Users` & `Roles` & `UserRoles`: Quản lý tài khoản, vai trò và phân quyền.
*   `AllowedEmails`: Danh sách email của giảng viên được phép đăng ký tài khoản.
*   `Subjects` & `Chapters`: Quản lý môn học và các chương tương ứng.
*   `Documents`: Lưu thông tin file tài liệu tải lên (Đường dẫn, mã băm MD5, kích thước, trạng thái index).
*   `DocumentChunks` & `Embeddings`: Lưu trữ nội dung tài liệu sau khi băm nhỏ và vector embedding tương ứng của từng đoạn.
*   `ChatSessions` & `ChatMessages`: Lưu trữ phiên chat và lịch sử hội thoại giữa người dùng với trợ lý ảo AI.
*   `DocumentConflicts`: Lưu thông tin các đoạn tài liệu bị trùng lặp hoặc xung đột nội dung.

### 2. Hướng dẫn cấu hình Kết nối (Connection String)
Mở tệp [appsettings.json](file:///c:/Users/Administrator/Desktop/Assignment_2/LearningDocumentSystem/LearningDocumentSystem.Web/appsettings.json) trong thư mục `LearningDocumentSystem.Web`:

```json
"ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LearningDocumentSystemDataBase;user id=sa;password=MẬT_KHẨU_SQL_CỦA_BẠN;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```
> [!IMPORTANT]
> Hãy thay thế `MẬT_KHẨU_SQL_CỦA_BẠN` bằng mật khẩu tài khoản `sa` trên máy của bạn. Nếu bạn sử dụng Windows Authentication (không dùng sa), bạn có thể đổi Connection String thành:
> `"Server=localhost;Database=LearningDocumentSystemDataBase;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"`

### 3. Tự động Khởi tạo Cơ sở dữ liệu (Database Seeding)
Khi bạn chạy dự án lần đầu tiên, hệ thống sẽ **tự động chạy Migrations** để tạo cơ sở dữ liệu trên SQL Server và thực thi **DataSeeder** để nạp sẵn dữ liệu thử nghiệm.

**Danh sách tài khoản mẫu để bạn đăng nhập thử nghiệm:**

| Tên Đăng Nhập | Mật Khẩu | Vai Trò (Role) | Chức Năng |
| :--- | :--- | :--- | :--- |
| **`admin`** | `Admin@123` | **Quản Trị Viên (Admin)** | Quản lý người dùng, duyệt email giảng viên, xem biểu đồ thống kê hệ thống |
| **`nguyenvan_gv`** | `Teacher@123` | **Giảng Viên (Teacher)** | Quản lý môn học, chương học, upload tài liệu học tập, xem tài liệu bị trùng lặp |
| **`tranmanh_sv`** | `Student@123` | **Sinh Viên (Student)** | Chat với trợ lý ảo AI để hỏi đáp dựa trên tài liệu của môn học/chương |

---

## 🛠️ Hướng Dẫn Chạy Dự Án (How to Run)

### Điều kiện cần có trên máy tính:
1.  **.NET 8 SDK** (Tải từ trang chủ Microsoft).
2.  **SQL Server** (Bản Developer hoặc Express) + **SSMS (SQL Server Management Studio)** để quản lý DB.
3.  Một công cụ lập trình như **Visual Studio 2022** (khuyên dùng) hoặc **Visual Studio Code**.

### Các bước khởi chạy:
1.  Mở Solution bằng Visual Studio: Nhấp đúp chuột vào tệp [LearningDocumentSystem.slnx](file:///c:/Users/Administrator/Desktop/Assignment_2/LearningDocumentSystem/LearningDocumentSystem.slnx).
2.  Cấu hình Connection String trong `appsettings.json` (như hướng dẫn ở mục trên).
3.  Nhấp nút **Start** (hoặc phím **F5**) trên Visual Studio để chạy dự án. Dự án khởi chạy mặc định sẽ mở trang web trên trình duyệt.

*(Nếu chạy bằng dòng lệnh CLI: Mở terminal tại thư mục `LearningDocumentSystem` và gõ lệnh `dotnet run --project LearningDocumentSystem.Web`)*

---

## 🤝 Hướng Dẫn Sử Dụng Git Cho Người Mới Bắt Đầu (Git Guide)

Nếu bạn chưa từng làm việc với Git, đừng lo lắng! Dưới đây là các hướng dẫn trực quan nhất để bạn có thể tải mã nguồn về và bắt đầu làm việc.

### Git là gì?
Git giống như một "cỗ máy thời gian" lưu trữ lịch sử mã nguồn của bạn. Mỗi lần bạn lưu code (gọi là **Commit**), Git sẽ chụp lại một bức ảnh trạng thái của toàn bộ dự án để bạn có thể khôi phục lại bất kỳ lúc nào nếu xảy ra lỗi.

---

### CÁCH 1: Sử Dụng GitHub Desktop (Dễ nhất - Khuyên dùng)
Bạn không cần gõ lệnh, chỉ cần nhấn chuột trên giao diện đồ họa trực quan.

#### Bước 1: Cài đặt công cụ
1.  Tải và cài đặt [GitHub Desktop](https://desktop.github.com/).
2.  Đăng nhập tài khoản GitHub của bạn (nếu có).

#### Bước 2: Tải dự án về máy (Clone)
1.  Mở GitHub Desktop, chọn **File** -> **Clone Repository**.
2.  Chọn tab **URL**, dán đường dẫn repository của bạn vào.
3.  Chọn thư mục trên máy tính muốn lưu dự án ở mục **Local Path**.
4.  Nhấn nút **Clone**.

#### Bước 3: Cập nhật code mới nhất từ nhóm về máy (Pull)
*   Trước khi bắt đầu sửa code, hãy luôn nhấn nút **Fetch origin** (ở góc trên cùng bên phải giao diện GitHub Desktop). Nếu có code mới từ người khác tải lên, nút này sẽ chuyển thành **Pull origin**. Hãy bấm vào đó để lấy code mới về máy.

#### Bước 4: Lưu thay đổi và gửi lên server (Commit & Push)
1.  Sau khi bạn sửa code, quay lại GitHub Desktop. Bạn sẽ thấy danh sách các tệp bị thay đổi ở cột bên trái.
2.  Ở góc dưới bên trái, điền tiêu đề mô tả thay đổi ở ô **Summary** (ví dụ: `Cập nhật cấu hình appsettings`).
3.  Bấm nút **Commit to main** (hoặc master).
4.  Nhấp nút **Push origin** ở trên cùng để gửi các thay đổi đó lên mạng cho mọi người cùng thấy.

---

### CÁCH 2: Sử Dụng Git Bằng Dòng Lệnh (Dành cho ai muốn học thêm)
Mở cửa sổ Command Prompt (CMD) hoặc PowerShell tại thư mục làm việc của bạn và gõ các câu lệnh sau:

#### 1. Lấy mã nguồn về máy lần đầu tiên (Clone)
```bash
git clone <ĐƯỜNG_DẪN_REPOSITORY>
```

#### 2. Cập nhật mã nguồn mới nhất từ trên mạng về (Pull)
*Hãy chạy lệnh này mỗi khi bạn bắt đầu làm việc trong ngày:*
```bash
git pull
```

#### 3. Kiểm tra các tệp bạn vừa sửa đổi (Status)
```bash
git status
```
*Lệnh này sẽ hiển thị các tệp màu đỏ (chưa được Git theo dõi) hoặc màu xanh (đã sẵn sàng).*

#### 4. Thêm các tệp đã sửa đổi vào hàng chờ (Add)
```bash
git add .
```
*(Dấu chấm đại diện cho việc thêm tất cả các tệp có thay đổi).*

#### 5. Lưu lại lịch sử kèm theo lời nhắn (Commit)
```bash
git commit -m "Mô tả ngắn gọn những gì bạn đã sửa đổi"
```

#### 6. Gửi code đã lưu lên server trên mạng (Push)
```bash
git push
```

---

### ⚠️ Quy Tắc Vàng Khi Dùng Git Để Tránh Lỗi (Conflict)
**Conflict (Xung đột)** xảy ra khi bạn và đồng đội cùng sửa trên một dòng code trong cùng một file, khiến Git không biết nên chọn dòng của ai. Để tránh điều này:
1.  **Luôn PULL trước khi CODE**: Đầu ngày làm việc, hãy Pull hoặc Fetch code mới nhất về.
2.  **Chia nhỏ công việc**: Hạn chế việc sửa chung một tệp tin với người khác cùng một lúc.
3.  Nếu gặp lỗi Xung đột (Conflict) khi push/pull, đừng hoảng loạn:
    *   Mở file bị báo lỗi lên (sẽ có các ký tự `<<<<<<< HEAD`, `=======`, `>>>>>>>`).
    *   Thảo luận với người sửa chung để chọn giữ lại code của ai hoặc gộp cả hai.
    *   Xóa các ký tự đánh dấu đó đi, lưu lại file, chạy lại lệnh `git add .`, `git commit` và `git push`.
