using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;
using System.Text.Json.Nodes;
using Serilog.Sinks.Grafana.Loki;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// 1. CẤU HÌNH SERILOG GỬI LOG VỀ LOKI & CONSOLE
// =========================================================================
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "Ocelot.Gateway")
    .Enrich.WithProperty("Environment", "Development")
    .WriteTo.Console()
    .WriteTo.GrafanaLoki("http://localhost:3100")
    .CreateLogger();

builder.Host.UseSerilog();

// =========================================================================
// 2. ĐĂNG KÝ OCELOT VÀ CORS (Ocelot đóng vai trò Pure Gateway Router)
// =========================================================================
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// =========================================================================
// 3. THIẾT LẬP PIPELINE MIDDLEWARE
// =========================================================================
app.UseCors("AllowAll");
app.UseRouting();

// CUSTOM MIDDLEWARE: TỰ ĐỘNG SINH ID NGẪU NHIÊN CHO REQUEST POST HÓA ĐƠN
app.Use(async (context, next) =>
{
    if (context.Request.Method == HttpMethods.Post &&
        context.Request.Path.Value != null &&
        context.Request.Path.Value.Contains("/gateway/InvoicesKhoiDn", StringComparison.OrdinalIgnoreCase))
    {
        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var bodyString = await reader.ReadToEndAsync();

        if (!string.IsNullOrEmpty(bodyString))
        {
            try
            {
                var jsonNode = JsonNode.Parse(bodyString);
                if (jsonNode != null)
                {
                    int generatedId = new Random().Next(1000, 9999);
                    jsonNode["parkingSessionId"] = generatedId;

                    var modifiedBody = jsonNode.ToJsonString();
                    var requestData = Encoding.UTF8.GetBytes(modifiedBody);
                    context.Request.Body = new MemoryStream(requestData);
                    context.Request.ContentLength = requestData.Length;
                }
                else
                {
                    context.Request.Body.Position = 0;
                }
            }
            catch
            {
                context.Request.Body.Position = 0;
            }
        }
        else
        {
            context.Request.Body.Position = 0;
        }
    }

    await next();
});

// KÍCH HOẠT OCELOT GATEWAY (Chuyển tiếp trọn vẹn Token xuống cho Microservice xử lý)
await app.UseOcelot();

app.Run();