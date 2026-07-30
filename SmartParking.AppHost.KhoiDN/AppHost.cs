var builder = DistributedApplication.CreateBuilder(args);

// 1. Đăng ký các Microservice và Trạm SignalR
var invoices = builder.AddProject<Projects.SmartParking_Invoices_Microservice_KhoiDN>("invoices-service");
var transactions = builder.AddProject<Projects.SmartParking_Transactions_Microservice_KhoiDN>("transactions-service");
var signalr = builder.AddProject<Projects.SmartParking_Notification_SignalRService_KhoiDN>("signalr-service");
var users = builder.AddProject<Projects.SmartParking_User_Microservice_KhoiDN>("user-service");

// 2. Đăng ký Ocelot API Gateway (ĐÃ THÊM CỔNG 5208 CHO ANDROID)
var gateway = builder.AddProject<Projects.SmartParking_OcelotAPIGateway_KhoiDN>("api-gateway")
                     .WithHttpEndpoint(port: 5000, name: "android-http");

// 3. Đăng ký Blazor Web App (Giao diện Frontend)
var webapp = builder.AddProject<Projects.SmartParking_MAUIHybridWebApp_KhoiDN_Web>("web-app");

// 4. Khởi chạy toàn bộ hệ thống
builder.Build().Run();