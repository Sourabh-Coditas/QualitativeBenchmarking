using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace KPMG.QualitativeBenchmarking.Api.Tests;

[Collection(nameof(ApiCollection))]
public class SavedSearchesTests : IClassFixture<ApiFixture>
{
    private readonly HttpClient _client;

    public SavedSearchesTests(ApiFixture fixture)
    {
        _client = fixture.CreateClient();
        _client.DefaultRequestHeaders.Add("X-User-Id", "22222222-2222-2222-2222-222222222222");
        _client.DefaultRequestHeaders.Add("X-Username", "Test User");
        _client.DefaultRequestHeaders.Add("X-Role", "User");
    }

    [Fact]
    public async Task GetStandard_Returns_200_And_Array()
    {
        var response = await _client.GetAsync("/api/saved-searches/standard");
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<List<SavedSearchItem>>();
        Assert.NotNull(list);
    }

    [Fact]
    public async Task GetCustomized_Returns_200_And_Array()
    {
        var response = await _client.GetAsync("/api/saved-searches/customized");
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<List<SavedSearchItem>>();
        Assert.NotNull(list);
    }

    [Fact]
    public async Task GetStandardByPeriod_Returns_200_And_Grouped_Array()
    {
        var response = await _client.GetAsync("/api/saved-searches/standard/by-period");
        response.EnsureSuccessStatusCode();

        var groups = await response.Content.ReadFromJsonAsync<List<SavedSearchPeriodGroup>>();
        Assert.NotNull(groups);
        foreach (var g in groups)
        {
            Assert.False(string.IsNullOrWhiteSpace(g.Period));
            Assert.NotNull(g.Items);
        }
    }

    [Fact]
    public async Task GetCustomizedByPeriod_Returns_200_And_Grouped_Array()
    {
        var response = await _client.GetAsync("/api/saved-searches/customized/by-period");
        response.EnsureSuccessStatusCode();

        var groups = await response.Content.ReadFromJsonAsync<List<SavedSearchPeriodGroup>>();
        Assert.NotNull(groups);
    }

    [Fact]
    public async Task Download_When_NotFound_Or_Not_Allowed_Returns_404()
    {
        var response = await _client.GetAsync($"/api/saved-searches/{Guid.NewGuid()}/download");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_Customized_Search_Returns_201()
    {
        var body = new
        {
            name = "My Custom Search",
            searchType = "Customized Search",
            financialYear = "FY 2024-25",
            transactionName = "IT Services"
        };
        var response = await _client.PostAsJsonAsync("/api/saved-searches", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_Standard_Search_As_Non_Admin_Returns_403()
    {
        var body = new
        {
            name = "Std Search",
            searchType = "Standard Search",
            financialYear = "FY 2024-25"
        };
        var response = await _client.PostAsJsonAsync("/api/saved-searches", body);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private sealed class SavedSearchItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string FinancialYear { get; set; } = "";
        public string RequestorName { get; set; } = "";
        public string SearchType { get; set; } = "";
    }

    private sealed class SavedSearchPeriodGroup
    {
        public string Period { get; set; } = "";
        public List<SavedSearchItem>? Items { get; set; }
    }
}
