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
    public class SystemUserAccountRepository : GenericRepository<SystemUserAccount> , ISystemUserAccountRepository
    {
        public SystemUserAccountRepository() { }
        public SystemUserAccountRepository(SmartParking_PaymentServiceContext context) : base(context)
        {
        }

        public async Task<SystemUserAccount> GetSystemUserAccountAsync(string username, string password)
        {

            return await _context.SystemUserAccounts.FirstOrDefaultAsync
                (x => (x.UserName == username || x.Email == username || x.Phone == username || x.EmployeeCode == username)
                      && x.Password == password
                      && x.IsActive);
        }



    }
}
