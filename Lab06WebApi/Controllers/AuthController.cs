using Lab06WebApi.Core.DTOs;
using Lab06WebApi.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lab06WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService service;
        public AuthController(IAuthService service)
        {
            this.service = service;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login (LoginDto dto)
        {
            var result = await service.LoginAsync(dto);
            if(result == null) return Unauthorized("Invalid username or password");
            return Ok(result);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            {
                return BadRequest(new { message = "RefreshToken is required." });
            }

            var result = await service.RefreshTokenAsync(dto.RefreshToken);
            if (result == null)
            {
                return Unauthorized(new { message = "Invalid or expired refresh token." });
            }

            return Ok(result);
        }

        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPost("revoke")]
        public async Task<IActionResult> Revoke()
        {
            var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (idClaim == null || !int.TryParse(idClaim.Value, out int accountId))
            {
                return Unauthorized();
            }

            var success = await service.RevokeTokenAsync(accountId);
            if (!success) return BadRequest(new { message = "Account not found." });
            return Ok(new { message = "Token revoked successfully." });
        }
    }
}
