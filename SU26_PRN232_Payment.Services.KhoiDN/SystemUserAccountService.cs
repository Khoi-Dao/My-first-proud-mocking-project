    using SU26_PRN232_Payment.Entities.KhoiDN.Models;
using SU26_PRN232_Payment.Repositories.KhoiDN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SU26_PRN232_Payment.Services.KhoiDN
{
    public class SystemUserAccountService : ISystemUserAccountService
    {
        // 1. Phụ thuộc vào Interface, không phụ thuộc vào Class cụ thể
        private readonly ISystemUserAccountRepository _repository;

        // 2. Sử dụng ILogger chuẩn của hệ thống
        private readonly ILogger<SystemUserAccountService> _logger;

        public SystemUserAccountService(
            ISystemUserAccountRepository repository,
            ILogger<SystemUserAccountService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SystemUserAccount> GetSystemUserAccountAsync(string username, string password)
        {
            // 3. Ghi log chuẩn xác (có phân loại mức độ Information, Warning, Error)
            _logger.LogInformation("Đang xử lý đăng nhập cho tài khoản: {Username}", username);

            // 4. Code logic sạch sẽ, KHÔNG có try/catch cồng kềnh. 
            // Nếu DB lỗi, nó sẽ văng thẳng lên Global Exception Middleware.
            var account = await _repository.GetSystemUserAccountAsync(username, password);

            if (account == null)
            {
                _logger.LogWarning("Đăng nhập thất bại. Không tìm thấy tài khoản hoặc sai mật khẩu: {Username}", username);
                return null;
            }

            _logger.LogInformation("Đăng nhập thành công: {Username}", username);

            return account;
        }
    }
}
