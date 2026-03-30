using KPMG.QualitativeBenchmarking.Application.Abstraction;
using KPMG.QualitativeBenchmarking.Application.Dtos.Feedback;
using KPMG.QualitativeBenchmarking.Domain.Entities;
using KPMG.QualitativeBenchmarking.Infrastructure.Data;

namespace KPMG.QualitativeBenchmarking.Infrastructure.Services;

public class FeedbackService : IFeedbackService
{
    private readonly DummyDataStore _store;

    public FeedbackService(DummyDataStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public Task<FeedbackDto> CreateAsync(CreateFeedbackDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.RequestId == Guid.Empty) throw new ArgumentException("RequestId is required.", nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.UserName)) throw new ArgumentException("UserName is required.", nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.Text)) throw new ArgumentException("Text is required.", nameof(dto));

        var req = _store.GetRequestById(dto.RequestId);
        if (req == null) throw new InvalidOperationException("Benchmarking request not found.");

        string displayName = dto.UserName.Trim();
        string? email = null;
        if (dto.UserId.HasValue)
        {
            var user = _store.GetUserById(dto.UserId.Value);
            if (user != null)
            {
                displayName = string.IsNullOrWhiteSpace(user.Name) ? displayName : user.Name.Trim();
                email = string.IsNullOrWhiteSpace(user.Email) ? null : user.Email.Trim();
            }
        }

        var entity = new Feedback
        {
            Id = Guid.NewGuid(),
            RequestId = dto.RequestId,
            UserId = dto.UserId,
            UserName = displayName,
            SubmitterEmail = email,
            SubmitterRole = string.IsNullOrWhiteSpace(dto.SubmitterRole) ? null : dto.SubmitterRole.Trim(),
            Text = dto.Text.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _store.AddFeedback(entity);
        return Task.FromResult(Map(entity));
    }

    public Task<IReadOnlyList<FeedbackDto>> GetByRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var list = _store.GetFeedbackByRequestId(requestId).Select(Map).ToList();
        return Task.FromResult<IReadOnlyList<FeedbackDto>>(list);
    }

    public Task<IReadOnlyList<FeedbackDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = _store.GetAllFeedback().Select(Map).ToList();
        return Task.FromResult<IReadOnlyList<FeedbackDto>>(list);
    }

    private static FeedbackDto Map(Feedback f)
    {
        return new FeedbackDto
        {
            Id = f.Id,
            RequestId = f.RequestId,
            UserId = f.UserId,
            SubmitterName = f.UserName,
            SubmitterEmail = f.SubmitterEmail,
            Role = f.SubmitterRole,
            SubmittedAtUtc = f.CreatedAtUtc,
            Text = f.Text
        };
    }
}

