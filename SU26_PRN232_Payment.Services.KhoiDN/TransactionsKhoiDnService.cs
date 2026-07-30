using Microsoft.Extensions.Logging;
using SU26_PRN232_Payment.Repositories.KhoiDN;
using SU26_PRN232_Payment.Services.KhoiDN.DTOs.Transaction;
using SU26_PRN232_Payment.Entities.KhoiDN.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SU26_PRN232_Payment.Services.KhoiDN
{
    public class TransactionsKhoiDnService : ITransactionsKhoiDnService
    {
        private readonly ITransactionsKhoiDnRepository _transactionRepository;
        private readonly IInvoicesKhoiDnRepository _invoiceRepository;
        private readonly ILogger<TransactionsKhoiDnService> _logger;

        // Inject cả 2 Repository độc lập qua Interface để xử lý logic liên bảng
        public TransactionsKhoiDnService(
            ITransactionsKhoiDnRepository transactionRepository,
            IInvoicesKhoiDnRepository invoiceRepository,
            ILogger<TransactionsKhoiDnService> logger)
        {
            _transactionRepository = transactionRepository;
            _invoiceRepository = invoiceRepository;
            _logger = logger;
        }

        public async Task<TransactionResponseDto> ProcessTransactionAsync(TransactionCreateRequestDto request)
        {
            _logger.LogInformation("Bắt đầu xử lý giao dịch thanh toán cho Hóa đơn ID: {InvoiceId}, Số tiền: {Amount}",
                request.InvoiceKhoiDnid, request.Amount);

            // 1. NGHIỆP VỤ: Kiểm tra xem Hóa đơn có tồn tại trong hệ thống không
            var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceKhoiDnid);
            if (invoice == null)
            {
                _logger.LogWarning("Xử lý thất bại. Không tìm thấy Hóa đơn ID: {InvoiceId}", request.InvoiceKhoiDnid);
                return null; // Trả về null để Controller ném lỗi 404 Not Found
            }

            // 2. MAPPING: Chuyển đổi dữ liệu từ Request DTO sang Entity
            var newTransaction = new TransactionsKhoiDn
            {
                InvoiceKhoiDnid = request.InvoiceKhoiDnid,
                Amount = request.Amount,
                PaymentMethod = request.PaymentMethod,
                ReferenceCode = request.ReferenceCode,

                // TỰ ĐỘNG SINH THÔNG TIN BẢO MẬT (Không tin tưởng Client)
                TransactionCode = $"TXN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}",
                PaymentStatus = "SUCCESS", // Giả định thành công cho luồng cơ bản (MVP)
                TransactionTime = DateTime.UtcNow
            };

            // 3. THỰC THI: Lưu bản ghi giao dịch mới vào bộ nhớ đệm
            await _transactionRepository.CreateAsync(newTransaction);

            // 4. LOGIC NGHIỆP VỤ LIÊN BẢNG (Cross-Entity Business Logic):
            // Nếu giao dịch thành công và số tiền khách trả đủ/vượt số tiền trên hóa đơn -> Cập nhật trạng thái Hóa đơn thành PAID
            if (newTransaction.PaymentStatus == "SUCCESS" && newTransaction.Amount >= invoice.TotalAmount)
            {
                invoice.Status = "PAID";
                _invoiceRepository.Update(invoice);
                _logger.LogInformation("Hóa đơn ID {InvoiceId} đã được thanh toán đủ. Đổi trạng thái sang PAID.", invoice.InvoiceKhoiDnid);
            }

            // 5. COMMIT: Lưu tất cả thay đổi (Thêm Transaction + Sửa Invoice) xuống Database trong cùng 1 Transaction SQL
            await _transactionRepository.SaveAsync();

            _logger.LogInformation("Xử lý giao dịch hoàn tất thành công. Mã giao dịch: {TxnCode}", newTransaction.TransactionCode);

            // 6. MAPPING: Trả dữ liệu an toàn về tầng Presentation
            return new TransactionResponseDto
            {
                TransactionCode = newTransaction.TransactionCode,
                InvoiceKhoiDnId = newTransaction.InvoiceKhoiDnid,
                Amount = newTransaction.Amount,
                PaymentMethod = newTransaction.PaymentMethod,
                PaymentStatus = newTransaction.PaymentStatus,
                TransactionTime = newTransaction.TransactionTime.GetValueOrDefault(),
                ReferenceCode = newTransaction.ReferenceCode
            };
        }

        public async Task<List<TransactionResponseDto>> GetTransactionsByInvoiceIdAsync(int invoiceId)
        {
            _logger.LogInformation("Đang truy xuất lịch sử giao dịch của Hóa đơn ID: {InvoiceId}", invoiceId);

            // Để hàm này chạy được, bạn cần bổ sung hàm GetByInvoiceIdAsync vào ITransactionsKhoiDnRepository
            // Hoặc tạm thời dùng GetAllAsync() của Generic rồi lọc bằng LINQ (Không khuyến khích ở Production lớn vì chậm)
            var allTransactions = await _transactionRepository.GetAllAsync();
            var filteredTransactions = allTransactions.Where(t => t.InvoiceKhoiDnid == invoiceId);

            return filteredTransactions.Select(t => new TransactionResponseDto
            {
                TransactionCode = t.TransactionCode,
                InvoiceKhoiDnId = t.InvoiceKhoiDnid,
                Amount = t.Amount,
                PaymentMethod = t.PaymentMethod,
                PaymentStatus = t.PaymentStatus,
                TransactionTime = t.TransactionTime.GetValueOrDefault(),
                ReferenceCode = t.ReferenceCode
            }).ToList();
        }
    }
}
