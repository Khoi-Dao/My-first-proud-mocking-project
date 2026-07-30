using Microsoft.AspNetCore.Mvc;
using SU26_PRN232_Payment.Services.KhoiDN;
using SU26_PRN232_Payment.Services.KhoiDN.DTOs; // Lưu ý: Namespace DTO của bạn hơi khác so với lúc nãy, nhớ giữ nguyên namespace đang chạy được của bạn nhé.
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text; // Bắt buộc phải có để dùng Encoding.UTF8

namespace SmartParking_PaymentService.APIWebApp.KhoiDN.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class LoginController : ControllerBase
    {
        private readonly ISystemUserAccountService _accountService;
        private readonly ILogger<LoginController> _logger;
        private readonly IConfiguration _configuration;

        // SỬA LỖI 1: Tiêm (Inject) đủ 3 Dependency vào Constructor
        public LoginController(
            ISystemUserAccountService accountService,
            ILogger<LoginController> logger,
            IConfiguration configuration)
        {
            _accountService = accountService;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponseDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            _logger.LogInformation("Nhận HTTP POST Request đăng nhập từ: {Username}", request.Username);

            var account = await _accountService.GetSystemUserAccountAsync(request.Username, request.Password);

            if (account == null)
            {
                _logger.LogWarning("Từ chối truy cập. Sai thông tin đăng nhập cho: {Username}", request.Username);
                return Unauthorized(new { message = "Tên đăng nhập hoặc mật khẩu không chính xác." });
            }

            // ==========================================
            // SỬA LỖI 2: THÊM LOGIC SINH JWT TOKEN TẠI ĐÂY
            // ==========================================
            var tokenHandler = new JwtSecurityTokenHandler();
            // Đọc Secret Key từ file appsettings.json
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, account.UserName),
                    new Claim(ClaimTypes.Role, account.RoleId.ToString()),
                    new Claim("UserId", account.UserAccountId.ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(2), // Thời hạn 2 tiếng
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwtString = tokenHandler.WriteToken(token); // Biến jwtString đã được tạo ra

            // ==========================================
            // MAPPING VÀ TRẢ VỀ KẾT QUẢ
            // ==========================================
            var response = new LoginResponseDto
            {
                UserAccountId = account.UserAccountId,
                UserName = account.UserName,
                FullName = account.FullName,
                Email = account.Email,
                Phone = account.Phone,
                RoleId = account.RoleId,
                AccessToken = jwtString // Gắn token vừa sinh ra vào phản hồi
            };

            _logger.LogInformation("Phản hồi thành công (200 OK) cho tài khoản: {Username}", account.UserName);

            return Ok(response);
        }
    }
}