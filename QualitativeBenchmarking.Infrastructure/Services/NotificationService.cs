using KPMG.QualitativeBenchmarking.Application.Abstraction;
using Microsoft.Extensions.Logging;

namespace KPMG.QualitativeBenchmarking.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task NotifyRequestStatusChangedAsync(
        Guid requestId,
        string benchmarkingName,
        string financialYear,
        string requestorName,
        string status,
        CancellationToken cancellationToken = default)
    {
        // Stub: replace with real email integration later.
        _logger.LogInformation(
            "Notification: request {RequestId} ({BenchmarkingName}, {FinancialYear}) for {RequestorName} is now {Status}",
            requestId,
            benchmarkingName,
            financialYear,
            requestorName,
            status);

        return Task.CompletedTask;
    }
}

