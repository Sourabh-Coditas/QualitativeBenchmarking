using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using KPMG.QualitativeBenchmarking.Application.Abstraction;
using KPMG.QualitativeBenchmarking.Application.Dtos.Ai;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace KPMG.QualitativeBenchmarking.Api.Tests;

/// <summary>
/// In-memory API host for integration tests. Uses a unique temp data file per fixture so tests don't conflict.
/// </summary>
public class ApiFixture : WebApplicationFactory<Program>
{
    private readonly string _testDataPath = Path.Combine(Path.GetTempPath(), "QualitativeBenchmarkingTests", $"data-{Guid.NewGuid():N}.json");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var dir = Path.GetDirectoryName(_testDataPath)!;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        builder.UseEnvironment(Environments.Development);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DummyData:FilePath"] = _testDataPath
            });
        });

        builder.ConfigureServices(services =>
        {
            // Prevent integration tests from calling the real external AI service.
            services.RemoveAll(typeof(IAiBenchmarkingService));
            services.AddScoped<IAiBenchmarkingService, FakeAiBenchmarkingService>();
        });
    }

    private sealed class FakeAiBenchmarkingService : IAiBenchmarkingService
    {
        private static readonly byte[] MinimalXlsx = new byte[] { 0x50, 0x4B, 0x05, 0x06, 0x00, 0x00, 0x00, 0x00 };

        public Task<AiServiceMetadataDto> GetMetadataAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AiServiceMetadataDto { Service = "FakeAi", Version = "1.0" });
        }

        public Task<AiHealthcheckDto> HealthcheckAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AiHealthcheckDto { Status = "ok", LlmReply = "ok" });
        }

        public Task<AiAnalyzeAllResponseDto> AnalyzeAllAsync(AiAnalyzeAllRequestDto request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AiAnalyzeAllResponseDto
            {
                MainReportPath = "fake://main_report",
                ReconReportPath = "fake://recon_report",
                DownloadMain = "fake://download_main",
                DownloadRecon = "fake://download_recon"
            });
        }

        public Task<AiGeneratePromptResponseDto> GeneratePromptAsync(AiGeneratePromptRequestDto request, CancellationToken cancellationToken = default)
        {
            var text = $"AI Prompt: {request.BusinessDescription.Trim()} | Exclusions: {request.ExclusionKeywords.Trim()}";
            return Task.FromResult(new AiGeneratePromptResponseDto { Prompt = text });
        }

        public Task<(Stream Content, string FileName)> DownloadAsync(
            AiDownloadRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var ms = new MemoryStream(MinimalXlsx);
            return Task.FromResult<(Stream, string)>((ms, request.PathOrDownloadUrl ?? "output.xlsx"));
        }
    }
}
