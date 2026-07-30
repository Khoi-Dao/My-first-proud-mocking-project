using Microsoft.AspNetCore.Components.Authorization;
using SmartParking.MAUIHybridWebApp.KhoiDN.Shared;
using SmartParking.MAUIHybridWebApp.KhoiDN.Shared.Services;
using SmartParking.MAUIHybridWebApp.KhoiDN.Web;
using SmartParking.MAUIHybridWebApp.KhoiDN.Web.Components;
using SmartParking.MAUIHybridWebApp.KhoiDN.Web.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- ĐĂNG KÝ HỆ THỐNG XÁC THỰC CỦA BLAZOR VÀ JWT ---
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "SmartParkingIssuer",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "SmartParkingAudience",
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "SmartParking_Super_Secret_Key_For_PRN232_Assignment5_2026!"))
    };
});

builder.Services.AddAuthorizationCore();
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add device-specific services used by the SmartParking.MAUIHybridWebApp.KhoiDN.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();

// =====================================================================
// CẤU HÌNH CHO WEB: Gọi qua Ocelot API Gateway (HTTPS localhost)
// =====================================================================
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7092/") });

builder.Services.AddHttpClient<InvoiceApiService>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7092/");
});
// =====================================================================

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// --- THỨ TỰ MIDDLEWARE BẮT BUỘC PHẢI ĐÚNG NHƯ SAU ---
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
// ---------------------------------------------------

app.MapStaticAssets();

// Gọi chính xác class App từ namespace Web.Components vừa import ở Dòng 1
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(SmartParking.MAUIHybridWebApp.KhoiDN.Shared.Services.IFormFactor).Assembly);

app.Run();