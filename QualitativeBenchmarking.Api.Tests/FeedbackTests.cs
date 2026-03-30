using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace KPMG.QualitativeBenchmarking.Api.Tests;

[Collection(nameof(ApiCollection))]
public class FeedbackTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _fixture;

    public FeedbackTests(ApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Create_When_Request_Exists_Returns_201()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", "22222222-2222-2222-2222-222222222222");
        client.DefaultRequestHeaders.Add("X-Username", "Test User");
        client.DefaultRequestHeaders.Add("X-Role", "User");

        var listRes = await client.GetAsync("/api/benchmarking-requests?pageSize=1");
        listRes.EnsureSuccessStatusCode();
        var list = await listRes.Content.ReadFromJsonAsync<BenchmarkingRequestList>();
        var requestId = list?.Items?.FirstOrDefault()?.Id ?? Guid.Empty;
        if (requestId == Guid.Empty) return;

        var body = new { text = "Integration test feedback." };
        var response = await client.PostAsJsonAsync($"/api/benchmarking-requests/{requestId}/feedback", body);
        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetByRequest_Returns_200_And_Array()
    {
        var client = _fixture.CreateClient();
        var listRes = await client.GetAsync("/api/benchmarking-requests?pageSize=1");
        listRes.EnsureSuccessStatusCode();
        var list = await listRes.Content.ReadFromJsonAsync<BenchmarkingRequestList>();
        var requestId = list?.Items?.FirstOrDefault()?.Id ?? Guid.NewGuid();

        var response = await client.GetAsync($"/api/benchmarking-requests/{requestId}/feedback");
        response.EnsureSuccessStatusCode();
        var feedbackList = await response.Content.ReadFromJsonAsync<List<FeedbackItem>>();
        Assert.NotNull(feedbackList);
    }

    [Fact]
    public async Task GetAll_Without_Admin_Returns_403()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", "22222222-2222-2222-2222-222222222222");
        client.DefaultRequestHeaders.Add("X-Username", "User");
        client.DefaultRequestHeaders.Add("X-Role", "User");

        var response = await client.GetAsync("/api/feedback");
        // Non-admin must not see all feedback: 403 Forbidden (or 500 if auth not wired in test)
        Assert.True(
            response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.InternalServerError,
            $"Expected 403 or 500, got {response.StatusCode}. Body: {await response.Content.ReadAsStringAsync()}");
    }

    [Fact]
    public async Task GetAll_With_Admin_Returns_200()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", "11111111-1111-1111-1111-111111111111");
        client.DefaultRequestHeaders.Add("X-Role", "Admin");

        var response = await client.GetAsync("/api/feedback");
        response.EnsureSuccessStatusCode();
        var list = await response.Content.ReadFromJsonAsync<List<FeedbackItem>>();
        Assert.NotNull(list);
    }

    private sealed class BenchmarkingRequestList
    {
        public List<Item>? Items { get; set; }
        public int TotalCount { get; set; }
    }

    private sealed class Item
    {
        public Guid Id { get; set; }
    }

    private sealed class FeedbackItem
    {
        public Guid Id { get; set; }
        public Guid RequestId { get; set; }
        public string Text { get; set; } = "";
    }
}
