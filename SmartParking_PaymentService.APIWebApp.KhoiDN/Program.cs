using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Sinks.Grafana.Loki;
using SU26_PRN232_Payment.Entities.KhoiDN.Models; // Namespace chứa DbContext
using SU26_PRN232_Payment.Repositories.KhoiDN;
using SU26_PRN232_Payment.Services.KhoiDN;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 2. CẤU HÌNH SERILOG BẮN LOG VỀ LOKI
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    // ⚠️ QUAN TRỌNG: Gắn nhãn "Service" để lát nữa lên Grafana lọc riêng được từng Microservice
    .Enrich.WithProperty("Service", "Invoices.Microservice")
    .Enrich.WithProperty("Environment", "Development")
    .WriteTo.Console() // Vẫn hiện log ra màn hình Console đen như bình thường
    .WriteTo.GrafanaLoki("http://localhost:3100") // <-- SỬA ĐÚNG LINK/PORT LOKI CỦA BẠN Ở ĐÂY
    .CreateLogger();

// 3. Ra lệnh cho ASP.NET dùng Serilog thay cho hệ thống log mặc định
builder.Host.UseSerilog();

// =========================================================================
// 1. CẤU HÌNH HỆ THỐNG & CƠ SỞ DỮ LIỆU
// =========================================================================
builder.Services.AddDbContext<SmartParking_PaymentServiceContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// =========================================================================
// 2. ĐĂNG KÝ LAYER REPOSITORIES (DATA ACCESS LAYER)
// =========================================================================
builder.Services.AddScoped<ISystemUserAccountRepository, SystemUserAccountRepository>();
builder.Services.AddScoped<IInvoicesKhoiDnRepository, InvoicesKhoiDnRepository>();
builder.Services.AddScoped<ITransactionsKhoiDnRepository, TransactionsKhoiDnRepository>();

// =========================================================================
// 3. ĐĂNG KÝ LAYER SERVICES (BUSINESS LOGIC LAYER)
// =========================================================================
builder.Services.AddScoped<ISystemUserAccountService, SystemUserAccountService>();
builder.Services.AddScoped<IInvoicesKhoiDnService, InvoicesKhoiDnService>();
builder.Services.AddScoped<ITransactionsKhoiDnService, TransactionsKhoiDnService>();

// =========================================================================
// 4. CẤU HÌNH ODATA, JWT & SWAGGER
// =========================================================================
builder.Services.AddControllers()
    .AddOData(options => options
        .Select()       // Cho phép chọn các cột muốn lấy ($select)
        .Filter()       // Cho phép lọc dữ liệu ($filter)
        .OrderBy()      // Cho phép sắp xếp ($orderby)
        .Expand()       // Cho phép lấy dữ liệu bảng con ($expand)
        .Count()        // Cho phép đếm số lượng ($count)
        .SetMaxTop(100) // Phân trang, tối đa lấy 100 dòng 1 lần ($top, $skip)
    );
builder.Services.AddEndpointsApiExplorer();

// ⚠️ ĐÃ SỬA: Đọc cấu hình JWT có chuỗi dự phòng (Fallback) để không bị lỗi null Audience/Issuer
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SmartParkingIssuer";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "SmartParkingAudience";
var jwtKey = builder.Configuration["Jwt:Key"] ?? "SmartParking_Super_Secret_Key_For_PRN232_Assignment5_2026!";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Smart Parking Invoices API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập token JWT của bạn vào đây. Ví dụ: eyJhbGciOiJIUzI1NiIs..."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});

// --- BỔ SUNG CẤU HÌNH MASSTRANSIT FOR INVOICES (PUBLISHER) ---
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest"); // Tài khoản mặc định của RabbitMQ
            h.Password("guest");
        });
    });
});
// -----------------------------------------------------------

var app = builder.Build();

// =========================================================================
// 5. CẤU HÌNH HTTP REQUEST PIPELINE
// =========================================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartParking Invoices API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();

// QUAN TRỌNG: UseAuthentication phải nằm TRƯỚC UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();