using Lab06WebApi.Core.DTOs;

namespace Lab06WebApi.Core.Services
{
    public interface ITransferService
    {
        Task<object> TransferAsync(int fromId, TransferDto dto);
        Task<object> RequestTransferOtpAsync(int fromAccountId, RequestTransferOtpDto dto);
        Task<object> ConfirmTransferOtpAsync(int fromAccountId, ConfirmTransferOtpDto dto);
        Task<List<object>> HistoryAsync(int accountId);
    }
}
