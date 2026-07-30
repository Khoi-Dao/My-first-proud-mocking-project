using Microsoft.Extensions.Logging;
using SmartParking.MAUIHybridWebApp.KhoiDN.Services;
using SmartParking.MAUIHybridWebApp.KhoiDN.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace SmartParking.MAUIHybridWebApp.KhoiDN
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            // Add device-specific services used by the SmartParking.MAUIHybridWebApp.KhoiDN.Shared project
            builder.Services.AddSingleton<IFormFactor, FormFactor>();

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            // =====================================================================
            // BỔ SUNG DI CHO XÁC THỰC
            // =====================================================================
            builder.Services.AddAuthorizationCore();
            builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
            // =====================================================================


            // Xác định URL Gateway dựa trên nền tảng
            string gatewayUrl = DeviceInfo.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:5000/"     // Cổng HTTP thường của Ocelot khi chạy trên Android Emulator
                : "https://localhost:7092/";  // Cổng HTTPS của Ocelot khi chạy trên Windows Machine


            // =====================================================================
            // 1. CẤU HÌNH HTTPCLIENT MẶC ĐỊNH (Dùng cho Login.razor / AuthStateProvider)
            // =====================================================================
            builder.Services.AddScoped(sp =>
            {
                var handler = new HttpClientHandler();
#if DEBUG
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
#endif
                return new HttpClient(handler) { BaseAddress = new Uri(gatewayUrl) };
            });

            // =====================================================================
            // 2. CẤU HÌNH HTTPCLIENT RIÊNG CHO INVOICE API SERVICE
            // =====================================================================
            builder.Services.AddHttpClient<InvoiceApiService>(client =>
            {
                client.BaseAddress = new Uri(gatewayUrl);
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler();
#if DEBUG
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
#endif
                return handler;
            });
            // =====================================================================

            return builder.Build();
        }
    }
}