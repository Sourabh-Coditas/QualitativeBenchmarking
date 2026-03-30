# Qualitative Benchmarking API – Integration tests

Runs the API in-memory (no Swagger/manual testing needed) and hits the main endpoints.

## Run tests

From repo root or `src`:

```bash
dotnet test QualitativeBenchmarking.Api.Tests/QualitativeBenchmarking.Api.Tests.csproj
```

Or from Visual Studio: **Test** → **Run All Tests**.

## What is tested

- **BenchmarkingRequests**: List, GetById (404), Create (multipart), validation (400), Update, Delete, Share, Trigger process, Download (404 when no output).
- **SavedSearches**: GET standard, GET customized, GET download (404).
- **Feedback**: Create, GetByRequest, GetAll (admin only → 403/500).

Tests use a shared in-memory host and a temp JSON data file so dev data is not modified. User context is set via headers (`X-User-Id`, `X-Username`, `X-Role`) for permission checks.
