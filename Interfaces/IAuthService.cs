using Todo_List_API.DTOs;

namespace Todo_List_API.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDTO registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDTO loginDto);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeTokenAsync(string token);
    }
}
