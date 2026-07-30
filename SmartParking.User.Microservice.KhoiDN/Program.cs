using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Sinks.Grafana.Loki;
using SU26_PRN232_Payment.Entities.KhoiDN.Models; // Namespace chứa DbContext của bạn
using SU26_PRN232_Payment.Repositories.KhoiDN;
using SU26_PRN232_Payment.Services.KhoiDN;

var builder = WebApplication.CreateBuilder(args);

// 1. CẤU HÌNH SERILOG BẮN LOG VỀ LOKI
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "User.Microservice") // Gắn nhãn riêng cho Service User
    .Enrich.WithProperty("Environment", "Development")
    .WriteTo.Console()
    .WriteTo.GrafanaLoki("http://localhost:3100") // Đảm bảo đúng port Loki của bạn
    .CreateLogger();

builder.Host.UseSerilog();

// 2. ĐĂNG KÝ DATABASE CONTEXT VÀ SERVICES CỦA HỆ THỐNG USER
// (Thay chuỗi kết nối bằng tên connection string trong appsettings.json của bạn)
builder.Services.AddDbContext<SmartParking_PaymentServiceContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ISystemUserAccountRepository, SystemUserAccountRepository>();
builder.Services.AddScoped<ISystemUserAccountService, SystemUserAccountService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();