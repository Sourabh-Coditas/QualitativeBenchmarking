using KPMG.QualitativeBenchmarking.Application.Dtos.Feedback;

namespace KPMG.QualitativeBenchmarking.Application.Abstraction;

public interface IFeedbackService
{
    Task<FeedbackDto> CreateAsync(CreateFeedbackDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeedbackDto>> GetByRequestAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeedbackDto>> GetAllAsync(CancellationToken cancellationToken = default);
}

