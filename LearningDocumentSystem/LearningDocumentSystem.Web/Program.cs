using LearningDocumentSystem.Web.Hubs;
using LearningDocumentSystem.Business.Mapping;
using System.Security.Claims;
using LearningDocumentSystem.Business.Services.Implementations;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Common.Constants;
using LearningDocumentSystem.Data.DbContexts;
using LearningDocumentSystem.Data.Repositories.Implementations;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using LearningDocumentSystem.Data.Seeders;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. DATABASE - Entity Framework Core + SQL Server
// ============================================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.MigrationsAssembly("LearningDocumentSystem.Data")));

// ============================================================
// 2. COOKIE AUTHENTICATION (thay ASP.NET Identity)
// ============================================================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath        = AppConstants.LoginPath;
        options.AccessDeniedPath = AppConstants.AccessDeniedPath;
        options.ExpireTimeSpan   = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name      = AppConstants.AuthCookieName;
        options.Cookie.HttpOnly  = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.None; // dev mode
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly",        p => p.RequireRole(AppConstants.RoleAdmin));
    options.AddPolicy("TeacherUp",        p => p.RequireAssertion(context =>
    {
        if (context.User.IsInRole(AppConstants.RoleAdmin)) return true;
        if (context.User.IsInRole(AppConstants.RoleTeacher))
        {
            var httpContext = context.Resource as HttpContext ?? new HttpContextAccessor().HttpContext;
            if (httpContext != null)
            {
                var db = httpContext.RequestServices.GetRequiredService<LearningDocumentSystem.Data.DbContexts.AppDbContext>();
                var userIdStr = context.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int userId))
                {
                    var user = db.Users.FirstOrDefault(u => u.UserID == userId);
                    if (user != null && (user.CanUpload || db.Subjects.Any(s => s.SubjectLeaderID == userId)))
                    {
                        return true;
                    }
                }
            }
            return context.User.HasClaim(c => c.Type == "CanUpload" && c.Value == "True");
        }
        return false;
    }));
    options.AddPolicy("TeacherOrStudent", p => p.RequireRole(AppConstants.RoleTeacher, AppConstants.RoleStudent));
    options.AddPolicy("AllUsers",        p => p.RequireAuthenticatedUser());
});

// ============================================================
// 3. SESSION
// ============================================================
builder.Services.AddSession(options =>
{
    options.IdleTimeout        = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly    = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();

// ============================================================
// 4. AUTOMAPPER
// ============================================================
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

// ============================================================
// 5. DEPENDENCY INJECTION - Repository + Services
// ============================================================
// Data layer
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Business layer
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<IChapterService, ChapterService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IChunkingService, ChunkingService>();
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<IChunkSettingsService, ChunkSettingsService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
// LLM Services & Factory
builder.Services.AddHttpClient<GeminiService>();
builder.Services.AddTransient<IGeminiService>(sp => sp.GetRequiredService<GeminiService>());
builder.Services.AddTransient<ILLMService>(sp => sp.GetRequiredService<GeminiService>());

builder.Services.AddHttpClient<OpenAiLlmService>();
builder.Services.AddTransient<ILLMService>(sp => sp.GetRequiredService<OpenAiLlmService>());

builder.Services.AddHttpClient<GroqLlmService>();
builder.Services.AddTransient<ILLMService>(sp => sp.GetRequiredService<GroqLlmService>());

builder.Services.AddScoped<ILLMProviderFactory, LLMProviderFactory>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IBenchmarkService, BenchmarkService>();

// Seeder
builder.Services.AddScoped<DataSeeder>();

// ============================================================
// 6. RAZOR PAGES & SIGNALR
// ============================================================
builder.Services.AddRazorPages()
    .AddRazorRuntimeCompilation();
builder.Services.AddSignalR();
builder.Services.AddScoped<INotificationService, LearningDocumentSystem.Web.Services.NotificationService>();

// Configure upload file size limit (50MB)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024;
});
builder.WebHost.ConfigureKestrel(k =>
{
    k.Limits.MaxRequestBodySize = 50 * 1024 * 1024;
});

// ============================================================
var app = builder.Build();
// ============================================================

// Seed database on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        log.LogError(ex, "Database seeding failed.");
    }
}

// Configure DocumentService upload path
app.Use(async (context, next) =>
{
    var docService = context.RequestServices.GetRequiredService<IDocumentService>();
    if (docService is DocumentService ds)
    {
        var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
        ds.SetUploadPath(Path.Combine(env.WebRootPath, AppConstants.UploadFolder));
    }
    await next();
});

// ============================================================
// Middleware pipeline
// ============================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// Routes
app.MapRazorPages();
app.MapHub<NotificationHub>("/notificationHub");

app.Run();
