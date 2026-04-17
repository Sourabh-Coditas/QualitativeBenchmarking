namespace FileStorageService.Core.Interfaces;

public interface IAssemblyThrottler
{
    Task<IDisposable> AcquireAsync(CancellationToken ct);

    int AvailableSlots { get; }

    int MaxConcurrentAssemblies { get; }
}
