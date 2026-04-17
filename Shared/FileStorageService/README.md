# File storage helper (DI module)

Copy this folder into any .NET solution: one registration call, no HTTP surface. Inject **`IStorageProvider`**, **`IChunkManager`**, and **`IAssemblyThrottler`** from your own modules or endpoints.

## Registered services

| Service | Role |
|---------|------|
| `IStorageProvider` | Stream or chunked upload (FileSystem / Azure Blob / AWS S3) |
| `IChunkManager` | In-memory sessions and chunk progress |
| `IAssemblyThrottler` | Limits concurrent chunk assembly |
| `IOptions<FileStorageOptions>` (+ Azure/S3 options) | Configuration |

## Project reference

```xml
<ItemGroup>
  <ProjectReference Include="path\to\FileStorageService\FileStorageService.csproj" />
</ItemGroup>
```

If the host does not pull dependencies transitively, mirror `PackageReference` entries from `FileStorageService.csproj`.

## Registration

```csharp
using FileStorageService;

builder.Services.AddFileStorageServices(builder.Configuration);
```

## Configuration

Merge **`FileStorage`**, **`AzureBlob`**, and **`AwsS3`** from `appsettings.snippet.json` into the host.

- **`FileStorage:StorageProvider`**: `FileSystem` | `AzureBlob` | `AwsS3`

## Example

```csharp
public sealed class MyUploadService(
    IStorageProvider storage,
    IChunkManager chunks,
    IAssemblyThrottler assembly)
{
    // storage.UploadStreamAsync uses PipeReader (e.g. Request.BodyReader in ASP.NET Core)
}
```

## Tests

```bash
dotnet test path/to/FileStorageService.Tests/FileStorageService.Tests.csproj
```

## Notes

- Chunk sessions are **in-memory**; scale-out may need a shared session strategy for chunked flows.
- No HTTP endpoints in this library.
