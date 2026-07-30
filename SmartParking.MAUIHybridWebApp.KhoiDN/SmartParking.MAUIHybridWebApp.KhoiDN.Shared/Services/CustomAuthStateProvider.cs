using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace SmartParking.MAUIHybridWebApp.KhoiDN.Shared.Services
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        private ClaimsPrincipal _currentUser;
        public string AuthToken { get; private set; } = string.Empty;

        public CustomAuthStateProvider()
        {
            _currentUser = _anonymous;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            return Task.FromResult(new AuthenticationState(_currentUser));
        }

        // Hàm này được gọi khi bấm nút Login thành công bên trang Login.razor
        public void MarkUserAsAuthenticated(string username, string token)
        {
            AuthToken = token;

            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, username)
            }, "JwtAuth");

            _currentUser = new ClaimsPrincipal(identity);

            // Báo cho toàn bộ ứng dụng Blazor vẽ lại giao diện theo trạng thái đã đăng nhập
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        // Hàm gọi khi bấm nút Logout
        public void MarkUserAsLoggedOut()
        {
            AuthToken = string.Empty;
            _currentUser = _anonymous;
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}