using Microsoft.AspNetCore.Components.Authorization;
using SmartParking.BusinessObject.Shared.Models.KhoiDN.DTOs;
using SmartParking.BusinessObject.Shared.Models.KhoiDN.DTOs.Transaction;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Linq; // Bổ sung để dùng được .Any() trong hàm AdvancedSearch

namespace SmartParking.MAUIHybridWebApp.KhoiDN.Shared.Services
{
    public class InvoiceApiService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authStateProvider;

        // Inject HttpClient và AuthenticationStateProvider vào Service
        public InvoiceApiService(HttpClient httpClient, AuthenticationStateProvider authStateProvider)
        {
            _httpClient = httpClient;
            _authStateProvider = authStateProvider;
        }

        // Hàm hỗ trợ tự động lấy Token và gắn vào Header trước mỗi lần gọi API
        private void AttachToken()
        {
            // Ép kiểu về CustomAuthStateProvider để lấy AuthToken đã lưu lúc Login
            if (_authStateProvider is CustomAuthStateProvider customProvider && !string.IsNullOrEmpty(customProvider.AuthToken))
            {
                // Gắn Header: Authorization: Bearer <token_jwt>
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customProvider.AuthToken);
            }
        }

        // 1. Gọi GET qua Gateway để lấy danh sách hóa đơn
        public async Task<List<InvoiceResponseDto>> GetAllInvoicesAsync()
        {
            try
            {
                AttachToken(); // <-- Gắn Token trước khi gửi request

                // Gọi vào route của Gateway (dùng relative path theo đúng config BaseAddress của bạn)
                var response = await _httpClient.GetFromJsonAsync<List<InvoiceResponseDto>>("gateway/InvoicesKhoiDn");
                return response ?? new List<InvoiceResponseDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi gọi Gateway: {ex.Message}");
                return new List<InvoiceResponseDto>();
            }
        }

        // 2. Gọi POST qua Gateway để tạo hóa đơn mới (Trả về số nguyên int: 1 là thành công, 0 là lỗi)
        public async Task<int> CreateInvoiceAsync(InvoiceCreateRequestDto request)
        {
            try
            {
                AttachToken();

                var response = await _httpClient.PostAsJsonAsync("gateway/InvoicesKhoiDn", request);
                if (response.IsSuccessStatusCode)
                {
                    // Đọc giá trị int trả về từ Controller (Ok(1))
                    return await response.Content.ReadFromJsonAsync<int>();
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi tạo hóa đơn: {ex.Message}");
                return 0;
            }
        }

        // 3. Gọi PUT qua Gateway để cập nhật
        public async Task<int> UpdateInvoiceAsync(int id, string? status, string? description)
        {
            try
            {
                AttachToken();
                // Tạo object chứa đầy đủ 3 thuộc tính khớp 100% với InvoiceUpdateRequestDto
                var updatePayload = new
                {
                    UserId = 1,
                    Status = status ?? "Paid",
                    Description = description ?? ""
                };

                var response = await _httpClient.PutAsJsonAsync($"gateway/InvoicesKhoiDn/{id}", updatePayload);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<int>();
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi cập nhật hóa đơn: {ex.Message}");
                return 0;
            }
        }

        // 4. Gọi DELETE qua Gateway để xóa hóa đơn (Trả về bool: true hoặc false)
        public async Task<bool> DeleteInvoiceAsync(int id)
        {
            try
            {
                AttachToken();
                var response = await _httpClient.DeleteAsync($"gateway/InvoicesKhoiDn/{id}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<bool>();
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi xóa hóa đơn: {ex.Message}");
                return false;
            }
        }

        // 5. Gọi GET qua Gateway để tìm kiếm nâng cao (Advanced Search)
        public async Task<List<InvoiceResponseDto>> AdvancedSearchAsync(int? parkingSessionId, string? status, string? description)
        {
            try
            {
                AttachToken();

                // 💡 CHỈ đưa vào URL những tham số có giá trị thật, bỏ qua tham số null/rỗng
                var queryParams = new List<string>();

                if (parkingSessionId.HasValue && parkingSessionId.Value > 0)
                    queryParams.Add($"parkingSessionId={parkingSessionId.Value}");

                if (!string.IsNullOrWhiteSpace(status))
                    queryParams.Add($"status={Uri.EscapeDataString(status.Trim())}");

                if (!string.IsNullOrWhiteSpace(description))
                    queryParams.Add($"description={Uri.EscapeDataString(description.Trim())}");

                // Ghép nối thành URL chuẩn: gateway/InvoicesKhoiDn/advancedsearch?status=PAID...
                string queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
                string url = $"gateway/InvoicesKhoiDn/advancedsearch{queryString}";

                var response = await _httpClient.GetFromJsonAsync<List<InvoiceResponseDto>>(url);
                return response ?? new List<InvoiceResponseDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi tìm kiếm nâng cao: {ex.Message}");
                return new List<InvoiceResponseDto>();
            }
        }

        // =========================================================================
        // 6. GỌI CHECK-OUT THUẦN EVENT-DRIVEN (THÔNG QUA INVOICES SERVICE)
        // =========================================================================
        public async Task<bool> ProcessCheckoutAsync(int invoiceId)
        {
            try
            {
                AttachToken();

                var updatePayload = new
                {
                    UserId = 1,
                    Status = "PAID",
                    Description = "Khách thanh toán CASH tại quầy (Check-out)"
                };

                var response = await _httpClient.PutAsJsonAsync($"gateway/InvoicesKhoiDn/{invoiceId}", updatePayload);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<int>();
                    return result == 1;
                }

                var errorMsg = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[CHECKOUT ERROR] HTTP {(int)response.StatusCode}: {errorMsg}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi gọi Check-out qua Gateway: {ex.Message}");
                return false;
            }
        }
    }
}