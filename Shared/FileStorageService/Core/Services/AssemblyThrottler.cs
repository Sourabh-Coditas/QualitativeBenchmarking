using FileStorageService.Core.Interfaces;
using FileStorageService.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileStorageService.Core.Services;

public class AssemblyThrottler : IAssemblyThrottler, IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private readonly ILogger<AssemblyThrottler> _logger;
    private readonly int _maxConcurrent;
    private bool _disposed;

    public AssemblyThrottler(
        IOptions<FileStorageOptions> options,
        ILogger<AssemblyThrottler> logger)
    {
        _maxConcurrent = options.Value.MaxConcurrentAssemblies;
        _semaphore = new SemaphoreSlim(_maxConcurrent, _maxConcurrent);
        _logger = logger;

        _logger.LogInformation(
            "AssemblyThrottler initialized with max {MaxConcurrent} concurrent assemblies",
            _maxConcurrent);
    }

    public int AvailableSlots => _semaphore.CurrentCount;

    public int MaxConcurrentAssemblies => _maxConcurrent;

    public async Task<IDisposable> AcquireAsync(CancellationToken ct)
    {
        var waitingCount = _maxConcurrent - _semaphore.CurrentCount;

        if (waitingCount > 0)
        {
            _logger.LogDebug(
                "Assembly slot requested. Currently {WaitingCount} assemblies in progress, waiting for slot...",
                waitingCount);
        }

        await _semaphore.WaitAsync(ct);

        _logger.LogDebug(
            "Assembly slot acquired. {Available}/{Max} slots now available",
            _semaphore.CurrentCount, _maxConcurrent);

        return new AssemblySlot(this);
    }

    private void Release()
    {
        if (_disposed) return;

        _semaphore.Release();

        _logger.LogDebug(
            "Assembly slot released. {Available}/{Max} slots now available",
            _semaphore.CurrentCount, _maxConcurrent);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _semaphore.Dispose();
    }

    private sealed class AssemblySlot : IDisposable
    {
        private readonly AssemblyThrottler _throttler;
        private bool _disposed;

        public AssemblySlot(AssemblyThrottler throttler)
        {
            _throttler = throttler;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _throttler.Release();
        }
    }
}
