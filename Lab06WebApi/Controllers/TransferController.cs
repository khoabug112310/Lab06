using Lab06WebApi.Core.DTOs;
using Lab06WebApi.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Lab06WebApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TransferController : ControllerBase
    {
        private readonly ITransferService service;
        public TransferController(ITransferService service)
        {
            this.service = service;
        }

        [HttpPost]
        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer(TransferDto dto)
        {
            try
            {
                var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                return Ok(await service.TransferAsync(id, dto));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("request-otp")]
        public async Task<IActionResult> RequestOtp(RequestTransferOtpDto dto)
        {
            try
            {
                var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                return Ok(await service.RequestTransferOtpAsync(id, dto));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("confirm-otp")]
        public async Task<IActionResult> ConfirmOtp(ConfirmTransferOtpDto dto)
        {
            try
            {
                var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                return Ok(await service.ConfirmTransferOtpAsync(id, dto));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> History()
        {
            try
            {
                var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                return Ok(await service.HistoryAsync(id));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
