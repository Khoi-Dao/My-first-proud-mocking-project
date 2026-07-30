using SU26_PRN232_Payment.Repositories.KhoiDN.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SU26_PRN232_Payment.Repositories.KhoiDN
{
    public class TransactionsKhoiDnRepository : GenericRepository<Entities.KhoiDN.Models.TransactionsKhoiDn>, ITransactionsKhoiDnRepository
    {
        

        public TransactionsKhoiDnRepository(SmartParking_PaymentServiceContext context)
            : base(context)
        {
        }

    }
}
