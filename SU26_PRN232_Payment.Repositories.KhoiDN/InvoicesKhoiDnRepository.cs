using Microsoft.EntityFrameworkCore;
using SU26_PRN232_Payment.Entities.KhoiDN.Models;
using SU26_PRN232_Payment.Repositories.KhoiDN.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SU26_PRN232_Payment.Repositories.KhoiDN
{
    public class InvoicesKhoiDnRepository: GenericRepository<Entities.KhoiDN.Models.InvoicesKhoiDn>, IInvoicesKhoiDnRepository
    {
        
        public InvoicesKhoiDnRepository(SmartParking_PaymentServiceContext context)
     : base(context)
        {
        }

        public async Task<InvoicesKhoiDn> GetInvoiceWithTransactionsAsync(int invoiceId)
        {
            // .Include() sẽ ra lệnh cho SQL Server JOIN bảng Invoices với bảng Transactions
            // Lưu ý: Đổi chữ 'TransactionsKhoiDns' cho khớp với tên navigation property trong file Models của bạn.
            return await _context.InvoicesKhoiDns
                .Include(i => i.TransactionsKhoiDns)
                .FirstOrDefaultAsync(i => i.InvoiceKhoiDnid == invoiceId);
        }
        public async Task<List<InvoicesKhoiDn>> SearchInvoicesAsync(string keyword)
        {
            // Nếu không nhập gì, trả về toàn bộ
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return await GetAllAsync();
            }

            keyword = keyword.ToLower();

            // EF Core sẽ tự động dịch đoạn này thành SQL: 
            // WHERE LOWER(InvoiceCode) LIKE '%keyword%' OR LOWER(Status) LIKE '%keyword%'...
            return await _context.InvoicesKhoiDns
                .Where(i => i.InvoiceCode.ToLower().Contains(keyword) ||
                            i.Status.ToLower().Contains(keyword) ||
                            (i.Description != null && i.Description.ToLower().Contains(keyword)))
                .ToListAsync();
        }
        public async Task<List<InvoicesKhoiDn>> SearchAdvancedAsync(int? parkingSessionId, string? status, string? description)
        {
            // Bắt đầu với toàn bộ dữ liệu chưa bị lọc
            var query = _context.InvoicesKhoiDns.AsQueryable();

            // 1. Nếu có truyền ParkingSessionId thì lọc thêm
            if (parkingSessionId.HasValue)
            {
                query = query.Where(i => i.ParkingSessionId == parkingSessionId.Value);
            }

            // 2. Nếu có truyền Status thì lọc thêm
            if (!string.IsNullOrWhiteSpace(status))
            {
                var statusLower = status.ToLower();
                query = query.Where(i => i.Status.ToLower().Contains(statusLower));
            }

            // 3. Nếu có truyền Description thì lọc thêm
            if (!string.IsNullOrWhiteSpace(description))
            {
                var descLower = description.ToLower();
                query = query.Where(i => i.Description != null && i.Description.ToLower().Contains(descLower));
            }

            // Sau khi đã nối xong các điều kiện, mới bắt đầu chạy xuống Database lấy dữ liệu
            return await query.ToListAsync();
        }
    }
}
