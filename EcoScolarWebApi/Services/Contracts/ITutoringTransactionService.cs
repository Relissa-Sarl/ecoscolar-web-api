using EcoScolarWebApi.Commun;
using EcoScolarWebApi.DTOs.Tutoring;

namespace EcoScolarWebApi.Services.Contracts;

public interface ITutoringTransactionService
{
    Task<Result> AcceptAsync(long transactionId, string sellerId);
    Task<Result> RefuseAsync(long transactionId, string sellerId);
    Task<Result> ConfirmAsync(long transactionId, string buyerId);
    Task<Result> MarkRenderedAsync(long transactionId, string sellerId);
    Task<Result<TutorContactDto>> GetTutorContactAsync(long transactionId, string buyerId);
}
