using Lab06WebApi.Core.DTOs;

namespace Lab06WebApi.Core.Services
{
    public interface IAuthService
    {
        Task<object?> LoginAsync(LoginDto dto);
        Task<object?> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeTokenAsync(int accountId);
    }
}
