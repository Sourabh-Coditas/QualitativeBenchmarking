using Xunit;

namespace KPMG.QualitativeBenchmarking.Api.Tests;

/// <summary>
/// Single shared API host for all integration tests to avoid parallel file access on the data store.
/// </summary>
[CollectionDefinition(nameof(ApiCollection))]
public class ApiCollection : ICollectionFixture<ApiFixture>
{
}
