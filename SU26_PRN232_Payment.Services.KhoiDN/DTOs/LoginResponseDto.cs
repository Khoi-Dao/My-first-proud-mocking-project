using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SU26_PRN232_Payment.Services.KhoiDN.DTOs
{
    public class LoginResponseDto
    {
        public int UserAccountId { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public int RoleId { get; set; }

        // Cố tình loại bỏ Password, RequestCode, IsActive và các trường Tracking (Created/Modified)

        // (Tùy chọn: Sau này bạn có thể bổ sung thêm Token JWT vào đây)
         public string AccessToken { get; set; } 
    }
}
