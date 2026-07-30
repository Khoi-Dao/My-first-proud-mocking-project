using MassTransit;
using Serilog;
using SmartParking.Notification.SignalRService.KhoiDN.Consumers;
using SmartParking.Notification.SignalRService.KhoiDN.Hubs;
using Serilog.Sinks.Grafana.Loki;

var builder = WebApplication.CreateBuilder(args);
// 2. CẤU HÌNH SERILOG BẮN LOG VỀ LOKI
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    // ⚠️ QUAN TRỌNG: Gắn nhãn "Service" để lát nữa lên Grafana lọc riêng được từng Microservice
    .Enrich.WithProperty("Service", "Notification.SignalR")
    .Enrich.WithProperty("Environment", "Development")
    .WriteTo.Console() // Vẫn hiện log ra màn hình Console đen như bình thường
    .WriteTo.GrafanaLoki("http://localhost:3100") // <-- SỬA ĐÚNG LINK/PORT LOKI CỦA BẠN Ở ĐÂY
    .CreateLogger();

// 3. Ra lệnh cho ASP.NET dùng Serilog thay cho hệ thống log mặc định
builder.Host.UseSerilog();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 1. Đăng ký dịch vụ SignalR
builder.Services.AddSignalR();

// 2. Cấu hình CORS (BẮT BUỘC để MAUI / Blazor App ở Port khác có thể kết nối WebSocket vào Hub)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClientApp", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

// 3. Cấu hình MassTransit & RabbitMQ cho Trạm Thông Báo
builder.Services.AddMassTransit(x =>
{
    // Đăng ký Consumer lắng nghe kết quả giao dịch
    x.AddConsumer<TransactionCompletedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // Tạo một Queue riêng tên là "NotificationQueue" để nhận tin từ Microservice 2
        cfg.ReceiveEndpoint("NotificationQueue", ep =>
        {
            ep.ConfigureConsumer<TransactionCompletedConsumer>(context);
        });
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowClientApp");
app.UseAuthorization();

app.MapControllers();

// 4. Mở đường dẫn (Endpoint) cho SignalR Hub
app.MapHub<NotificationHub>("/notificationHub");

app.Run();