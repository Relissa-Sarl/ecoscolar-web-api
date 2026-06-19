namespace EcoScolarWebApi.Services.Contracts;

/// <summary>
/// Processes due tutoring packages held in escrow:
/// refunds packages whose acceptance deadline passed, and releases funds to the tutor for
/// packages confirmed by the student, auto-released after the tutor's delay, or expired.
/// </summary>
public interface ITutoringEscrowProcessor
{
    Task ProcessDueTransactionsAsync(CancellationToken cancellationToken = default);
}
