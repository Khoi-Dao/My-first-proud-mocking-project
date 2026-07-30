using SU26_PRN232_Payment.Repositories.KhoiDN.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SU26_PRN232_Payment.Repositories.KhoiDN
{
    public interface IInvoicesKhoiDnRepository : IGenericRepository<Entities.KhoiDN.Models.InvoicesKhoiDn>
    {
        Task<Entities.KhoiDN.Models.InvoicesKhoiDn> GetInvoiceWithTransactionsAsync(int invoiceId);

        Task<List<Entities.KhoiDN.Models.InvoicesKhoiDn>> SearchInvoicesAsync(string keyword);
        Task<List<Entities.KhoiDN.Models.InvoicesKhoiDn>> SearchAdvancedAsync(int? parkingSessionId, string? status, string? description);
    }
}
