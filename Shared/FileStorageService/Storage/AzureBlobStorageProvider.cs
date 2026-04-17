using System.IO.Pipelines;
using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using FileStorageService.Core.Interfaces;
using FileStorageService.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileStorageService.Storage;

public class AzureBlobStorageProvider : IStorageProvider
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<AzureBlobStorageProvider> _logger;

    public AzureBlobStorageProvider(
        IOptions<AzureBlobOptions> azureOptions,
        ILogger<AzureBlobStorageProvider> logger)
    {
        _logger = logger;

        var options = azureOptions.Value;
        var blobServiceClient = new BlobServiceClient(options.ConnectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(options.ContainerName);

        _containerClient.CreateIfNotExists();
    }

    public async Task<string> UploadStreamAsync(string fileName, PipeReader data, CancellationToken ct)
    {
        var blobName = GetUniqueBlobName(fileName);
        var blobClient = _containerClient.GetBlobClient(blobName);

        _logger.LogDebug("Starting streaming upload to Azure Blob: {BlobName}", blobName);

        await using var stream = data.AsStream();

        var uploadOptions = new BlobUploadOptions
        {
            TransferOptions = new Azure.Storage.StorageTransferOptions
            {
                MaximumConcurrency = 4,
                MaximumTransferSize = 50 * 1024 * 1024
            }
        };

        await blobClient.UploadAsync(stream, uploadOptions, ct);

        _logger.LogInformation("Streaming upload to Azure Blob completed: {BlobUri}", blobClient.Uri);

        return blobClient.Uri.ToString();
    }

    public async Task UploadChunkAsync(string uploadId, int chunkIndex, Stream data, CancellationToken ct)
    {
        var chunkBlobName = $"chunks/{uploadId}/chunk_{chunkIndex:D6}";
        var blobClient = _containerClient.GetBlobClient(chunkBlobName);

        _logger.LogDebug("Uploading chunk {ChunkIndex} to Azure Blob: {BlobName}", chunkIndex, chunkBlobName);

        await blobClient.UploadAsync(data, overwrite: true, ct);

        _logger.LogDebug("Chunk {ChunkIndex} uploaded to Azure Blob", chunkIndex);
    }

    public async Task<string> AssembleChunksAsync(string uploadId, int totalChunks, string fileName, long fileSizeBytes, CancellationToken ct)
    {
        var finalBlobName = GetUniqueBlobName(fileName);
        var blockBlobClient = _containerClient.GetBlockBlobClient(finalBlobName);

        _logger.LogInformation(
            "Assembling {TotalChunks} chunks ({FileSize:N0} bytes) into Azure Blob: {BlobName} using server-side copy",
            totalChunks, fileSizeBytes, finalBlobName);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var blockIds = new List<string>();

        for (var i = 0; i < totalChunks; i++)
        {
            ct.ThrowIfCancellationRequested();

            var chunkBlobName = $"chunks/{uploadId}/chunk_{i:D6}";
            var chunkBlobClient = _containerClient.GetBlobClient(chunkBlobName);

            var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes($"block-{i:D6}"));
            blockIds.Add(blockId);

            await blockBlobClient.StageBlockFromUriAsync(
                chunkBlobClient.Uri,
                blockId,
                cancellationToken: ct);

            if ((i + 1) % 10 == 0 || i == totalChunks - 1)
            {
                var progress = (i + 1) * 100 / totalChunks;
                _logger.LogDebug(
                    "Azure Blob assembly progress: {Progress}% ({Current}/{Total} chunks staged)",
                    progress, i + 1, totalChunks);
            }
        }

        await blockBlobClient.CommitBlockListAsync(blockIds, cancellationToken: ct);

        stopwatch.Stop();
        var throughputMBps = (fileSizeBytes / (1024.0 * 1024.0)) / (stopwatch.ElapsedMilliseconds / 1000.0);

        _logger.LogInformation(
            "Azure Blob assembly completed: {BlobUri}, Duration: {Duration}ms, Throughput: {Throughput:N1} MB/s (server-side)",
            blockBlobClient.Uri, stopwatch.ElapsedMilliseconds, throughputMBps);

        return blockBlobClient.Uri.ToString();
    }

    public async Task DeleteChunksAsync(string uploadId, CancellationToken ct)
    {
        var prefix = $"chunks/{uploadId}/";

        _logger.LogDebug("Deleting Azure Blob chunks with prefix: {Prefix}", prefix);

        await foreach (var blobItem in _containerClient.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix, ct))
        {
            var blobClient = _containerClient.GetBlobClient(blobItem.Name);
            await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
        }

        _logger.LogDebug("Azure Blob chunks deleted for upload: {UploadId}", uploadId);
    }

    public async Task<bool> ExistsAsync(string fileName, CancellationToken ct)
    {
        var searchPattern = Path.GetFileNameWithoutExtension(fileName);

        await foreach (var blobItem in _containerClient.GetBlobsAsync(BlobTraits.None, BlobStates.None, searchPattern, ct))
        {
            if (blobItem.Name.Contains(fileName))
            {
                return true;
            }
        }

        return false;
    }

    public async Task DeleteAsync(string fileName, CancellationToken ct)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);

        _logger.LogDebug("Deleting Azure Blob: {BlobName}", fileName);

        await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
    }

    private static string GetUniqueBlobName(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var guidPart = Guid.NewGuid().ToString("N")[..8];
        return $"{baseName}_{DateTime.UtcNow:yyyyMMddHHmmss}_{guidPart}{extension}";
    }
}
