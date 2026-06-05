using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Domain.Interfaces;

public interface IFeedbackRepository
{
    Task AddAsync(FeedbackSubmission feedback, CancellationToken cancellationToken = default);
    Task<IEnumerable<FeedbackSubmission>> GetAsync(
        Guid? branchId, DateTime? from, DateTime? to,
        CancellationToken cancellationToken = default);
}
