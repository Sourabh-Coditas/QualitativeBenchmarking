namespace KPMG.QualitativeBenchmarking.Application.Abstraction;

public interface INotificationService
{
    Task NotifyRequestStatusChangedAsync(
        Guid requestId,
        string benchmarkingName,
        string financialYear,
        string requestorName,
        string status,
        CancellationToken cancellationToken = default);
}

