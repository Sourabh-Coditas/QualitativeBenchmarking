# Shared `FileStorageService` (copy-paste module)

This folder contains the **generic** `FileStorageService` class library. **Do not change it** to suit one application—hosts wrap it (see `QualitativeBenchmarking.Infrastructure`).

## Moving to another solution

1. Copy the entire **`FileStorageService`** directory next to your host projects (or under `src/Shared/`).
2. Add a **project reference** from your host or infrastructure project to `FileStorageService/FileStorageService.csproj`.
3. At startup, call **`AddFileStorageServices(configuration)`** (namespace `FileStorageService`) so `IStorageProvider`, `IChunkManager`, and `IAssemblyThrottler` are registered.
4. Merge **`FileStorage`**, **`AzureBlob`**, and **`AwsS3`** from `appsettings.snippet.json` into your configuration.
5. Implement an **application adapter** in your solution that injects `IStorageProvider` (only) for app-level file APIs—do not reference Azure/AWS SDKs from feature code if you want the same boundaries as Qualitative Benchmarking.

## Qualitative Benchmarking usage

This product uses **`FileSystem`** or **`AzureBlob`** only (`FileStorage:StorageProvider`). The adapter **`SharedFileStorageModule`** + **`QualitativeFileStorageService`** live in the Infrastructure project, not here.
