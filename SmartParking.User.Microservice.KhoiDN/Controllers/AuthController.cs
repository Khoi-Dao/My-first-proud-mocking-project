using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SmartParking.BusinessObject.Shared.Models.KhoiDN.DTOs;
using SU26_PRN232_Payment.Services.KhoiDN;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SmartParking.User.Microservice.KhoiDN.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ISystemUserAccountService _accountService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ISystemUserAccountService accountService, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _accountService = accountService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            _logger.LogInformation("Đang kiểm tra đăng nhập cho user: {Username}", request.Username);

            // Gọi hàm thực tế từ tầng Service của bạn
            var account = await _accountService.GetSystemUserAccountAsync(request.Username, request.Password);

            if (account == null)
            {
                _logger.LogWarning("Đăng nhập thất bại cho user: {Username}", request.Username);
                return Unauthorized(new { Message = "Tài khoản hoặc mật khẩu không chính xác!" });
            }

            // Sinh Token JWT khi đăng nhập thành công
            var jwtKey = _configuration["Jwt:Key"] ?? "SmartParking_Super_Secret_Key_For_PRN232_Assignment5_2026!";
            var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, account.UserAccountId.ToString()),
                new Claim(ClaimTypes.Name, account.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, account.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, account.RoleId.ToString()),
                new Claim("FullName", account.FullName ?? string.Empty)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),

                // ⚠️ BỔ SUNG 2 DÒNG QUAN TRỌNG NÀY ĐỂ FIX LỖI EMPTY AUDIENCE ⚠️
                Issuer = _configuration["Jwt:Issuer"] ?? "SmartParkingIssuer",
                Audience = _configuration["Jwt:Audience"] ?? "SmartParkingAudience",
                // ---------------------------------------------------------------

                Expires = DateTime.UtcNow.AddHours(8),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            var accessTokenString = tokenHandler.WriteToken(securityToken);

            var responseDto = new LoginResponseDto
            {
                UserAccountId = account.UserAccountId,
                UserName = account.UserName,
                FullName = account.FullName,
                Email = account.Email,
                Phone = account.Phone,
                RoleId = account.RoleId,
                AccessToken = accessTokenString
            };

            _logger.LogInformation("Đăng nhập thành công và đã cấp JWT cho: {Username}", account.UserName);
            return Ok(responseDto);
        }
    }
}