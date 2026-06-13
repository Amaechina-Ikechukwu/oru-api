using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Azure.Identity;
using ORUApi.Data;
using ORUApi.Services;
using ORUApi.Endpoints.Applications;
using ORUApi.Endpoints.Students;
using ORUApi.Endpoints.Announcements;
using ORUApi.Endpoints.Auth;
using ORUApi.Endpoints.Webhook;
using ORUApi.Endpoints.Courses;
using ORUApi.Endpoints.StudyLevels;
using ORUApi.Endpoints.Health;

var builder = WebApplication.CreateBuilder(args);

// Key Vault — only in production when a vault name is configured
var kvName = builder.Configuration["KeyVault:Name"];
if (!string.IsNullOrEmpty(kvName))
{
    try
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri($"https://{kvName}.vault.azure.net/"),
            new DefaultAzureCredential());
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[WARN] Could not connect to Azure Key Vault '{kvName}': {ex.Message}");
        Console.WriteLine("[WARN] Continuing without Key Vault — using appsettings.json values instead.");
    }
}

// DB
var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(
    builder.Configuration.GetConnectionString("DefaultConnection")!);
dataSourceBuilder.EnableDynamicJson();
builder.Services.AddDbContext<ORUDbContext>(opt =>
    opt.UseNpgsql(dataSourceBuilder.Build()));

// JWT Auth (two schemes: student + admin)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt => {
        opt.TokenValidationParameters = new() {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization(opt => {
    opt.AddPolicy("SuperAdmin", p => p.RequireRole("SuperAdmin"));
    opt.AddPolicy("AdminOnly", p => p.RequireRole("Admin", "SuperAdmin"));
    opt.AddPolicy("StudentOnly", p => p.RequireRole("Student"));
    opt.AddPolicy("Admissions", p => p.RequireRole("SuperAdmin", "AdmissionsOfficer"));
    opt.AddPolicy("Bursar", p => p.RequireRole("SuperAdmin", "Bursar"));
    opt.AddPolicy("AcademicAdvisor", p => p.RequireRole("SuperAdmin", "AcademicAdvisor"));
});

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<BlobStorageService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter your JWT token"
    });
    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", doc)] = new List<string>()
    });
});

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.ConfigObject.PersistAuthorization = true;
});
app.UseAuthentication();
app.UseAuthorization();

// Register endpoint groups
app.MapApplicationEndpoints();
app.MapStudentEndpoints();
app.MapAnnouncementEndpoints();
app.MapCourseEndpoints();
app.MapStudyLevelEndpoints();
app.MapAuthEndpoints();
app.MapWebhookEndpoints();
app.MapHealthEndpoints();

// Seed default SuperAdmin if none exists
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ORUDbContext>();
    db.Database.Migrate();

    Guid adminId;

    if (!db.Admins.Any())
    {
        var cfg = app.Configuration.GetSection("DefaultAdmin");
        var admin = new ORUApi.Models.Admin
        {
            Email = cfg["Email"]!,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(cfg["Password"]!),
            StaffId = cfg["StaffId"]!,
            FullName = cfg["FullName"]!,
            Role = ORUApi.Models.AdminRole.SuperAdmin,
            Permissions = new()
            {
                CanApproveApplications = true,
                CanPostAnnouncements = true,
                CanUpdateGrades = true,
                CanManageFinance = true,
                CanManageAdmins = true
            },
            IsActive = true
        };
        db.Admins.Add(admin);
        db.SaveChanges();
        adminId = admin.Id;
    }
    else
    {
        adminId = db.Admins.First().Id;
    }

    // Seed default courses if none exist
    if (!db.Courses.Any())
    {
        var courses = new[]
        {
            new ORUApi.Models.Course { Code = "THE101", Title = "Theology & Ministry", Description = "Introduction to theological studies and ministry practice", CreditUnits = 3, CreatedByAdminId = adminId },
            new ORUApi.Models.Course { Code = "LDS101", Title = "Leadership Studies", Description = "Foundations of leadership theory and practice", CreditUnits = 3, CreatedByAdminId = adminId },
            new ORUApi.Models.Course { Code = "BAP101", Title = "Business Administration & Philosophy", Description = "Principles of business administration and philosophical foundations", CreditUnits = 3, CreatedByAdminId = adminId },
            new ORUApi.Models.Course { Code = "EDU101", Title = "Education (Peace land University Approved)", Description = "Foundations of education approved by Peace land University", CreditUnits = 3, CreatedByAdminId = adminId },
            new ORUApi.Models.Course { Code = "MGT101", Title = "Management Courses", Description = "Introduction to management principles and practices", CreditUnits = 3, CreatedByAdminId = adminId },
            new ORUApi.Models.Course { Code = "CSC101", Title = "Computer Science", Description = "Introduction to computer science fundamentals", CreditUnits = 3, CreatedByAdminId = adminId },
            new ORUApi.Models.Course { Code = "GCN101", Title = "Guidance & Counseling / Christian Studies", Description = "Principles of guidance, counseling and Christian studies", CreditUnits = 3, CreatedByAdminId = adminId },
            new ORUApi.Models.Course { Code = "CRS101", Title = "Conflict Resolution & Peace Studies", Description = "Introduction to conflict resolution and peace building", CreditUnits = 3, CreatedByAdminId = adminId },
            new ORUApi.Models.Course { Code = "PCP101", Title = "Professional Certification Programs", Description = "Professional certification and career development", CreditUnits = 3, CreatedByAdminId = adminId },
        };
        db.Courses.AddRange(courses);
        db.SaveChanges();
    }

    // Seed study levels if none exist
    if (!db.StudyLevels.Any())
    {
        var levels = new[]
        {
            new ORUApi.Models.StudyLevel { Name = "Certificate", Description = "Professional Certifications & Short-Term Courses (3-6 months or 1 year)", Duration = "3-12 months", SortOrder = 1 },
            new ORUApi.Models.StudyLevel { Name = "Diploma", Description = "Diploma Programs (2 years)", Duration = "2 years", SortOrder = 2 },
            new ORUApi.Models.StudyLevel { Name = "Undergraduate", Description = "Undergraduate / Bachelor's Degree (4 years)", Duration = "4 years", SortOrder = 3 },
            new ORUApi.Models.StudyLevel { Name = "Postgraduate Diploma", Description = "Postgraduate Diploma (PGDE, PDE)", Duration = "1-2 years", SortOrder = 4 },
            new ORUApi.Models.StudyLevel { Name = "Masters", Description = "Master's Degree (M.Ed., etc.)", Duration = "1-2 years", SortOrder = 5 },
        };
        db.StudyLevels.AddRange(levels);
        db.SaveChanges();
    }
}

app.Run();