using MassTransit;
using Microsoft.EntityFrameworkCore; // BỔ SUNG: Thư viện EF Core để kết nối SQL
using Serilog;
using SmartParking.Transactions.Microservice.KhoiDN.Consumers;
using SU26_PRN232_Payment.Entities.KhoiDN.Models; // BỔ SUNG: Gọi DbContext từ tầng Entities
using SU26_PRN232_Payment.Repositories.KhoiDN;
using SU26_PRN232_Payment.Services.KhoiDN;
using Serilog.Sinks.Grafana.Loki;

var builder = WebApplication.CreateBuilder(args);
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    // ⚠️ QUAN TRỌNG: Gắn nhãn "Service" để lát nữa lên Grafana lọc riêng được từng Microservice
    .Enrich.WithProperty("Service", "Transactions.Microservice")
    .Enrich.WithProperty("Environment", "Development")
    .WriteTo.Console() // Vẫn hiện log ra màn hình Console đen như bình thường
    .WriteTo.GrafanaLoki("http://localhost:3100") // <-- SỬA ĐÚNG LINK/PORT LOKI CỦA BẠN Ở ĐÂY
    .CreateLogger();

// 3. Ra lệnh cho ASP.NET dùng Serilog thay cho hệ thống log mặc định
builder.Host.UseSerilog();
// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Smart Parking Transactions API", Version = "v1" });
});

// =========================================================================
// 1. ĐĂNG KÝ CƠ SỞ DỮ LIỆU & REPOSITORY & SERVICE TỪ ASSIGNMENT 1
// =========================================================================
// BỔ SUNG QUAN TRỌNG: Đăng ký DbContext để Repository gọi được SQL Server
builder.Services.AddDbContext<SmartParking_PaymentServiceContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IInvoicesKhoiDnRepository, InvoicesKhoiDnRepository>();
// Đăng ký Repository (Tầng kết nối DB)
builder.Services.AddScoped<ITransactionsKhoiDnRepository, TransactionsKhoiDnRepository>();

// Đăng ký Service (Tầng nghiệp vụ)
builder.Services.AddScoped<ITransactionsKhoiDnService, TransactionsKhoiDnService>();

// =========================================================================
// 2. CẤU HÌNH MASSTRANSIT FOR TRANSACTIONS (CONSUMER)
// =========================================================================
builder.Services.AddMassTransit(x =>
{
    // 1. Đăng ký Consumer vào hệ thống DI
    x.AddConsumer<TransactionsKhoiDnConsumer>();

    // 2. Cấu hình kết nối tới RabbitMQ và mở hàng đợi lắng nghe
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // Cấu hình Queue "InvoicesQueue" để hút tin nhắn từ Microservice 1 về
        cfg.ReceiveEndpoint("InvoicesQueue", ep =>
        {
            ep.PrefetchCount = 16; // Giới hạn xử lý tối đa 16 hóa đơn cùng lúc để tránh tràn RAM
            ep.UseMessageRetry(r => r.Interval(2, 100)); // Tự động thử lại 2 lần nếu DB bận/lỗi
            ep.ConfigureConsumer<TransactionsKhoiDnConsumer>(context);
        });
    });
});
// -------------------------------------------------------------------------

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();