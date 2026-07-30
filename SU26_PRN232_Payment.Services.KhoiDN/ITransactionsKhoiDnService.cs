using SU26_PRN232_Payment.Services.KhoiDN.DTOs.Transaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SU26_PRN232_Payment.Services.KhoiDN
{
    public interface ITransactionsKhoiDnService
    {
        // 1. Nghiệp vụ cốt lõi: Xử lý một giao dịch thanh toán mới
        // Nhận vào Request DTO (từ Frontend) và trả ra Response DTO (đã xử lý xong)
        Task<TransactionResponseDto> ProcessTransactionAsync(TransactionCreateRequestDto request);

        // 2. Nghiệp vụ phụ trợ (Nên có): Lấy toàn bộ lịch sử giao dịch của 1 hóa đơn
        // Dùng để Frontend hiển thị danh sách các lần quẹt thẻ/chuyển khoản trên màn hình chi tiết
        Task<List<TransactionResponseDto>> GetTransactionsByInvoiceIdAsync(int invoiceId);
    }
}
