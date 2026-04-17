using System.Collections.Concurrent;
using System.IO.Pipelines;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using FileStorageService.Core.Interfaces;
using FileStorageService.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileStorageService.Storage;

public class AwsS3StorageProvider : IStorageProvider
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly ILogger<AwsS3StorageProvider> _logger;

    private readonly ConcurrentDictionary<string, string> _multipartUploadIds = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, string>> _partETags = new();

    public AwsS3StorageProvider(
        IOptions<AwsS3Options> options,
        ILogger<AwsS3StorageProvider> logger)
    {
        _logger = logger;
        var s3Options = options.Value;
        _bucketName = s3Options.BucketName;

        var config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(s3Options.Region)
        };

        if (!string.IsNullOrEmpty(s3Options.ServiceUrl))
        {
            config.ServiceURL = s3Options.ServiceUrl;
            config.ForcePathStyle = true;
        }

        _s3Client = new AmazonS3Client(s3Options.AccessKey, s3Options.SecretKey, config);
    }

    public async Task<string> UploadStreamAsync(string fileName, PipeReader data, CancellationToken ct)
    {
        var key = GetUniqueKey(fileName);

        _logger.LogDebug("Starting streaming upload to S3: {Key}", key);

        var transferUtility = new TransferUtility(_s3Client);

        await using var stream = data.AsStream();

        var uploadRequest = new TransferUtilityUploadRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = stream,
            AutoCloseStream = false,
            PartSize = 50 * 1024 * 1024
        };

        await transferUtility.UploadAsync(uploadRequest, ct);

        var url = $"s3://{_bucketName}/{key}";
        _logger.LogInformation("Streaming upload to S3 completed: {Url}", url);

        return url;
    }

    public async Task UploadChunkAsync(string uploadId, int chunkIndex, Stream data, CancellationToken ct)
    {
        if (!_multipartUploadIds.ContainsKey(uploadId))
        {
            var initRequest = new InitiateMultipartUploadRequest
            {
                BucketName = _bucketName,
                Key = $"uploads/{uploadId}"
            };

            var initResponse = await _s3Client.InitiateMultipartUploadAsync(initRequest, ct);
            _multipartUploadIds[uploadId] = initResponse.UploadId;
            _partETags[uploadId] = new ConcurrentDictionary<int, string>();

            _logger.LogDebug("Initiated S3 multipart upload: {UploadId}, S3UploadId: {S3UploadId}",
                uploadId, initResponse.UploadId);
        }

        var s3UploadId = _multipartUploadIds[uploadId];
        var partNumber = chunkIndex + 1;

        _logger.LogDebug("Uploading S3 part {PartNumber} for upload: {UploadId}", partNumber, uploadId);

        using var memoryStream = new MemoryStream();
        await data.CopyToAsync(memoryStream, ct);
        memoryStream.Position = 0;

        var uploadPartRequest = new UploadPartRequest
        {
            BucketName = _bucketName,
            Key = $"uploads/{uploadId}",
            UploadId = s3UploadId,
            PartNumber = partNumber,
            InputStream = memoryStream
        };

        var uploadPartResponse = await _s3Client.UploadPartAsync(uploadPartRequest, ct);

        _partETags[uploadId][chunkIndex] = uploadPartResponse.ETag;

        _logger.LogDebug("S3 part {PartNumber} uploaded, ETag: {ETag}", partNumber, uploadPartResponse.ETag);
    }

    public async Task<string> AssembleChunksAsync(string uploadId, int totalChunks, string fileName, long fileSizeBytes, CancellationToken ct)
    {
        if (!_multipartUploadIds.TryGetValue(uploadId, out var s3UploadId))
        {
            throw new InvalidOperationException($"No multipart upload found for uploadId: {uploadId}");
        }

        _logger.LogInformation(
            "Completing S3 multipart upload: {UploadId}, TotalParts: {TotalChunks}, Size: {FileSize:N0} bytes (server-side assembly)",
            uploadId, totalChunks, fileSizeBytes);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var partETags = _partETags[uploadId];
        var partResponses = new List<PartETag>();

        for (var i = 0; i < totalChunks; i++)
        {
            if (!partETags.TryGetValue(i, out var eTag))
            {
                throw new InvalidOperationException($"Missing ETag for chunk {i}");
            }

            partResponses.Add(new PartETag(i + 1, eTag));
        }

        var completeRequest = new CompleteMultipartUploadRequest
        {
            BucketName = _bucketName,
            Key = $"uploads/{uploadId}",
            UploadId = s3UploadId,
            PartETags = partResponses
        };

        await _s3Client.CompleteMultipartUploadAsync(completeRequest, ct);

        _logger.LogDebug("S3 multipart upload assembled, copying to final location...");

        var finalKey = GetUniqueKey(fileName);
        var copyRequest = new CopyObjectRequest
        {
            SourceBucket = _bucketName,
            SourceKey = $"uploads/{uploadId}",
            DestinationBucket = _bucketName,
            DestinationKey = finalKey
        };

        await _s3Client.CopyObjectAsync(copyRequest, ct);

        await _s3Client.DeleteObjectAsync(_bucketName, $"uploads/{uploadId}", ct);

        stopwatch.Stop();
        var throughputMBps = (fileSizeBytes / (1024.0 * 1024.0)) / (stopwatch.ElapsedMilliseconds / 1000.0);

        var url = $"s3://{_bucketName}/{finalKey}";
        _logger.LogInformation(
            "S3 multipart upload completed: {Url}, Duration: {Duration}ms, Throughput: {Throughput:N1} MB/s (server-side)",
            url, stopwatch.ElapsedMilliseconds, throughputMBps);

        _multipartUploadIds.TryRemove(uploadId, out _);
        _partETags.TryRemove(uploadId, out _);

        return url;
    }

    public async Task DeleteChunksAsync(string uploadId, CancellationToken ct)
    {
        _logger.LogDebug("Cleaning up S3 multipart upload: {UploadId}", uploadId);

        if (_multipartUploadIds.TryRemove(uploadId, out var s3UploadId))
        {
            try
            {
                var abortRequest = new AbortMultipartUploadRequest
                {
                    BucketName = _bucketName,
                    Key = $"uploads/{uploadId}",
                    UploadId = s3UploadId
                };

                await _s3Client.AbortMultipartUploadAsync(abortRequest, ct);
                _logger.LogDebug("Aborted S3 multipart upload: {S3UploadId}", s3UploadId);
            }
            catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchUpload")
            {
                _logger.LogDebug("S3 multipart upload already completed or aborted: {S3UploadId}", s3UploadId);
            }
        }

        _partETags.TryRemove(uploadId, out _);
    }

    public async Task<bool> ExistsAsync(string fileName, CancellationToken ct)
    {
        try
        {
            var request = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = Path.GetFileNameWithoutExtension(fileName),
                MaxKeys = 1
            };

            var response = await _s3Client.ListObjectsV2Async(request, ct);
            return response.S3Objects.Count > 0;
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchBucket")
        {
            return false;
        }
    }

    public async Task DeleteAsync(string fileName, CancellationToken ct)
    {
        _logger.LogDebug("Deleting S3 object: {Key}", fileName);

        await _s3Client.DeleteObjectAsync(_bucketName, fileName, ct);
    }

    private static string GetUniqueKey(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var guidPart = Guid.NewGuid().ToString("N")[..8];
        return $"files/{baseName}_{DateTime.UtcNow:yyyyMMddHHmmss}_{guidPart}{extension}";
    }
}
