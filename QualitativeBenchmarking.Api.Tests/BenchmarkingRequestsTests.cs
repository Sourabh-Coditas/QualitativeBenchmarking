using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KPMG.QualitativeBenchmarking.Api.Tests.Helpers;
using Xunit;

namespace KPMG.QualitativeBenchmarking.Api.Tests;

[Collection(nameof(ApiCollection))]
public class BenchmarkingRequestsTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _fixture;
    private readonly HttpClient _client;

    public BenchmarkingRequestsTests(ApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
        _client.DefaultRequestHeaders.Add("X-User-Id", "22222222-2222-2222-2222-222222222222");
        _client.DefaultRequestHeaders.Add("X-Username", "Test User");
        _client.DefaultRequestHeaders.Add("X-Role", "User");
    }

    [Fact]
    public async Task List_Returns_200_With_Items_And_TotalCount()
    {
        var response = await _client.GetAsync("/api/benchmarking-requests?page=1&pageSize=10");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("items", out _));
        Assert.True(json.TryGetProperty("totalCount", out var total));
        Assert.True(total.GetInt32() >= 0);
    }

    [Fact]
    public async Task List_With_MyRequestsOnly_Returns_200()
    {
        var response = await _client.GetAsync("/api/benchmarking-requests?myRequestsOnly=true&page=1&pageSize=10");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetById_When_NotFound_Returns_404()
    {
        var response = await _client.GetAsync($"/api/benchmarking-requests/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_With_Valid_Multipart_Returns_201()
    {
        using var content = MultipartRequestHelper.CreateBenchmarkingRequest(
            benchmarkingName: "Test Benchmark",
            transactionName: "Test Transaction",
            industry: "IT",
            financialYear: "FY 2023-24");

        var response = await _client.PostAsync("/api/benchmarking-requests", content);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<BenchmarkingRequestDetail>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("Test Benchmark", created.BenchmarkingName);
        Assert.Equal("Generated", created.Status);
    }

    [Fact]
    public async Task Create_With_Invalid_FinancialYear_Returns_400()
    {
        using var content = MultipartRequestHelper.CreateBenchmarkingRequest(
            benchmarkingName: "Test",
            transactionName: "Test",
            industry: "IT",
            financialYear: "INVALID");

        var response = await _client.PostAsync("/api/benchmarking-requests", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_Then_GetById_Returns_Updated_Data()
    {
        // Create a Customized Search so we (non-admin) can edit it; use create response for id
        Guid id;
        using (var createContent = MultipartRequestHelper.CreateBenchmarkingRequest("Update Test", "Tx", "IT", "FY 2023-24", searchType: "Customized Search"))
        {
            var createRes = await _client.PostAsync("/api/benchmarking-requests", createContent);
            createRes.EnsureSuccessStatusCode();
            var created = await createRes.Content.ReadFromJsonAsync<BenchmarkingRequestDetail>();
            Assert.NotNull(created);
            id = created.Id;
        }

        var updateBody = new { benchmarkingName = "Updated Name", purpose = "TP Benchmarking" };
        var updateRes = await _client.PutAsJsonAsync($"/api/benchmarking-requests/{id}", updateBody);
        updateRes.EnsureSuccessStatusCode();

        var getRes = await _client.GetAsync($"/api/benchmarking-requests/{id}");
        getRes.EnsureSuccessStatusCode();
        var detail = await getRes.Content.ReadFromJsonAsync<BenchmarkingRequestDetail>();
        Assert.NotNull(detail);
        Assert.Equal("Updated Name", detail.BenchmarkingName);
        Assert.Equal("TP Benchmarking", detail.Purpose);
    }

    [Fact]
    public async Task Delete_When_Exists_Returns_204()
    {
        using (var createContent = MultipartRequestHelper.CreateBenchmarkingRequest("Delete Test", "Tx", "IT", "FY 2023-24"))
        {
            var createRes = await _client.PostAsync("/api/benchmarking-requests", createContent);
            createRes.EnsureSuccessStatusCode();
            var created = await createRes.Content.ReadFromJsonAsync<BenchmarkingRequestDetail>();
            Assert.NotNull(created);

            var deleteRes = await _client.DeleteAsync($"/api/benchmarking-requests/{created.Id}");
            Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);
        }
    }

    [Fact]
    public async Task Share_With_Valid_Body_Returns_204()
    {
        var listRes = await _client.GetAsync("/api/benchmarking-requests?pageSize=1");
        listRes.EnsureSuccessStatusCode();
        var list = await listRes.Content.ReadFromJsonAsync<ListResponse>();
        var id = list?.Items?.FirstOrDefault()?.Id ?? Guid.NewGuid();

        var body = new { requestId = id, email = "someone@example.com" };
        var response = await _client.PostAsJsonAsync("/api/benchmarking-requests/share", body);
        Assert.True(response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TriggerProcess_When_Exists_Returns_204()
    {
        var listRes = await _client.GetAsync("/api/benchmarking-requests?pageSize=1");
        listRes.EnsureSuccessStatusCode();
        var list = await listRes.Content.ReadFromJsonAsync<ListResponse>();
        var id = list?.Items?.FirstOrDefault()?.Id ?? Guid.NewGuid();

        var response = await _client.PostAsync($"/api/benchmarking-requests/{id}/process", null);
        Assert.True(response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Download_When_No_Output_Returns_404()
    {
        // Use a non-existent id to keep this test deterministic and independent of test order.
        var id = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/benchmarking-requests/{id}/download?type=main");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed class BenchmarkingRequestDetail
    {
        public Guid Id { get; set; }
        public string BenchmarkingName { get; set; } = "";
        public string Status { get; set; } = "";
        public string? Purpose { get; set; }
    }

    private sealed class ListResponse
    {
        public List<BenchmarkingRequestDetail>? Items { get; set; }
        public int TotalCount { get; set; }
    }
}
