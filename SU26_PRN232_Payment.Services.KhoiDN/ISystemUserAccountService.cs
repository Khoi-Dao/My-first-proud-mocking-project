using SU26_PRN232_Payment.Entities.KhoiDN.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SU26_PRN232_Payment.Services.KhoiDN
{
    public interface ISystemUserAccountService
    {
         Task<SystemUserAccount> GetSystemUserAccountAsync(string username, string password);
    }
}
