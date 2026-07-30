using SU26_PRN232_Payment.Services.KhoiDN.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SU26_PRN232_Payment.Services.KhoiDN
{
    public interface IInvoicesKhoiDnService
    {
        // 1. Hàm CRUD cơ bản (Dùng để demo cho giảng viên)
        Task<InvoiceResponseDto> CreateInvoiceAsync(InvoiceCreateRequestDto request);

        // 2. Hàm Nghiệp vụ thực chiến (Dùng cho nút Check-in Bãi xe)
        Task<InvoiceResponseDto> CreateBusinessInvoiceAsync(InvoiceCreateRequestDto request);

        Task<InvoiceDetailResponseDto> GetInvoiceByIdAsync(int id);
        Task<List<InvoiceResponseDto>> GetAllInvoicesAsync();
        Task<bool> UpdateInvoiceAsync(int id, string status, string description);
        Task<bool> DeleteInvoiceAsync(int id);
        Task<List<InvoiceResponseDto>> SearchInvoicesAsync(string keyword);
        Task<List<InvoiceResponseDto>> SearchAdvancedAsync(int? parkingSessionId, string? status, string? description);
    }
}