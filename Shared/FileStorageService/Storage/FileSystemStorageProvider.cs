using System.IO.Pipelines;
using FileStorageService.Core.Interfaces;
using FileStorageService.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileStorageService.Storage;

public class FileSystemStorageProvider : IStorageProvider
{
    private readonly FileStorageOptions _options;
    private readonly ILogger<FileSystemStorageProvider> _logger;
    private readonly string _uploadDirectory;
    private readonly string _tempDirectory;

    public FileSystemStorageProvider(
        IOptions<FileStorageOptions> options,
        ILogger<FileSystemStorageProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
        _uploadDirectory = Path.GetFullPath(_options.UploadDirectory);
        _tempDirectory = Path.GetFullPath(_options.TempDirectory);

        Directory.CreateDirectory(_uploadDirectory);
        Directory.CreateDirectory(_tempDirectory);
    }

    public async Task<string> UploadStreamAsync(string fileName, PipeReader data, CancellationToken ct)
    {
        var safeFileName = GetSafeFileName(fileName);
        var filePath = Path.Combine(_uploadDirectory, safeFileName);

        _logger.LogDebug("Starting streaming upload to: {FilePath}", filePath);

        await using var fileStream = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        while (true)
        {
            var result = await data.ReadAsync(ct);
            var buffer = result.Buffer;

            foreach (var segment in buffer)
            {
                await fileStream.WriteAsync(segment, ct);
            }

            data.AdvanceTo(buffer.End);

            if (result.IsCompleted)
            {
                break;
            }
        }

        _logger.LogInformation("Streaming upload completed: {FilePath}, Size: {Size} bytes",
            filePath, fileStream.Length);

        return filePath;
    }

    public async Task UploadChunkAsync(string uploadId, int chunkIndex, Stream data, CancellationToken ct)
    {
        var chunkDirectory = GetChunkDirectory(uploadId);
        Directory.CreateDirectory(chunkDirectory);

        var chunkPath = GetChunkPath(uploadId, chunkIndex);

        _logger.LogDebug("Uploading chunk {ChunkIndex} to: {ChunkPath}", chunkIndex, chunkPath);

        await using var fileStream = new FileStream(
            chunkPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await data.CopyToAsync(fileStream, ct);

        _logger.LogDebug("Chunk {ChunkIndex} uploaded: {Size} bytes", chunkIndex, fileStream.Length);
    }

    private const int LargeFileBufferSize = 1024 * 1024;

    public async Task<string> AssembleChunksAsync(string uploadId, int totalChunks, string fileName, long fileSizeBytes, CancellationToken ct)
    {
        var safeFileName = GetSafeFileName(fileName);
        var finalPath = Path.Combine(_uploadDirectory, safeFileName);

        _logger.LogInformation(
            "Assembling {TotalChunks} chunks ({FileSize:N0} bytes) into: {FinalPath}",
            totalChunks, fileSizeBytes, finalPath);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        await using var outputStream = new FileStream(
            finalPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: LargeFileBufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        if (fileSizeBytes > 0)
        {
            outputStream.SetLength(fileSizeBytes);
            _logger.LogDebug("Pre-allocated {FileSize:N0} bytes for output file", fileSizeBytes);
        }

        long bytesWritten = 0;
        var lastProgressLog = 0;

        for (var i = 0; i < totalChunks; i++)
        {
            ct.ThrowIfCancellationRequested();

            var chunkPath = GetChunkPath(uploadId, i);

            if (!File.Exists(chunkPath))
            {
                throw new FileNotFoundException($"Chunk {i} not found at path: {chunkPath}");
            }

            await using var chunkStream = new FileStream(
                chunkPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: LargeFileBufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            var chunkSize = chunkStream.Length;
            await chunkStream.CopyToAsync(outputStream, LargeFileBufferSize, ct);
            bytesWritten += chunkSize;

            var progress = (int)((i + 1) * 100 / totalChunks);
            if (progress >= lastProgressLog + 10 || i == totalChunks - 1)
            {
                _logger.LogDebug(
                    "Assembly progress: {Progress}% ({Current}/{Total} chunks, {BytesWritten:N0} bytes)",
                    progress, i + 1, totalChunks, bytesWritten);
                lastProgressLog = progress;
            }
        }

        stopwatch.Stop();
        var throughputMBps = (bytesWritten / (1024.0 * 1024.0)) / (stopwatch.ElapsedMilliseconds / 1000.0);

        _logger.LogInformation(
            "Assembly completed: {FinalPath}, Size: {Size:N0} bytes, Duration: {Duration}ms, Throughput: {Throughput:N1} MB/s",
            finalPath, outputStream.Length, stopwatch.ElapsedMilliseconds, throughputMBps);

        return finalPath;
    }

    public Task DeleteChunksAsync(string uploadId, CancellationToken ct)
    {
        var chunkDirectory = GetChunkDirectory(uploadId);

        if (Directory.Exists(chunkDirectory))
        {
            _logger.LogDebug("Deleting chunk directory: {ChunkDirectory}", chunkDirectory);
            Directory.Delete(chunkDirectory, recursive: true);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string fileName, CancellationToken ct)
    {
        var filePath = Path.IsPathRooted(fileName)
            ? fileName
            : Path.Combine(_uploadDirectory, fileName);
        return Task.FromResult(File.Exists(filePath));
    }

    public Task DeleteAsync(string fileName, CancellationToken ct)
    {
        var filePath = Path.IsPathRooted(fileName)
            ? fileName
            : Path.Combine(_uploadDirectory, fileName);

        if (File.Exists(filePath))
        {
            _logger.LogDebug("Deleting file: {FilePath}", filePath);
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    private string GetChunkDirectory(string uploadId)
    {
        return Path.Combine(_tempDirectory, uploadId);
    }

    private string GetChunkPath(string uploadId, int chunkIndex)
    {
        return Path.Combine(GetChunkDirectory(uploadId), $"chunk_{chunkIndex:D6}");
    }

    private static string GetSafeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        var invalidChars = Path.GetInvalidFileNameChars();

        foreach (var c in invalidChars)
        {
            name = name.Replace(c, '_');
        }

        var extension = Path.GetExtension(name);
        var baseName = Path.GetFileNameWithoutExtension(name);
        var guidPart = Guid.NewGuid().ToString("N")[..8];
        return $"{baseName}_{DateTime.UtcNow:yyyyMMddHHmmss}_{guidPart}{extension}";
    }
}
