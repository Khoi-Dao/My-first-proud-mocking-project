using SU26_PRN232_Payment.Entities.KhoiDN.Models;
using SU26_PRN232_Payment.Repositories.KhoiDN.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SU26_PRN232_Payment.Repositories.KhoiDN
{
    public interface ISystemUserAccountRepository : IGenericRepository<SystemUserAccount>
    {
        Task<SystemUserAccount> GetSystemUserAccountAsync(string username, string password);
    }
}
