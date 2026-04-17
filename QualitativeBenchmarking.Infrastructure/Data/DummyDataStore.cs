using System.Text.Json;
using KPMG.QualitativeBenchmarking.Domain.Entities;
using KPMG.QualitativeBenchmarking.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace KPMG.QualitativeBenchmarking.Infrastructure.Data;

/// <summary>
/// In-memory store backed by a JSON file. Loads on startup; saves after every mutation.
/// No database.
/// </summary>
public class DummyDataStore
{
    private readonly string _filePath;
    private readonly object _lock = new();
    private List<User> _users = new();
    private List<BenchmarkingRequest> _requests = new();
    private List<SavedSearch> _searches = new();
    private List<PromptRecord> _prompts = new();
    private List<Feedback> _feedbacks = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DummyDataStore(IOptions<DummyDataFileSettings> options)
    {
        var path = options?.Value?.FilePath ?? "Data/dummy-data.json";
        _filePath = Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
    }

    public void EnsureLoaded()
    {
        lock (_lock)
        {
            if (_users.Count > 0 || _requests.Count > 0 || _searches.Count > 0 || _prompts.Count > 0 || _feedbacks.Count > 0) return;
            if (File.Exists(_filePath))
            {
                try
                {
                    var json = File.ReadAllText(_filePath);
                    var model = JsonSerializer.Deserialize<DummyDataFileModel>(json, JsonOptions);
                    if (model != null)
                    {
                        _users = model.Users ?? new List<User>();
                        _requests = model.Requests ?? new List<BenchmarkingRequest>();
                        _searches = model.Searches ?? new List<SavedSearch>();
                        _prompts = model.Prompts ?? new List<PromptRecord>();
                        _feedbacks = model.Feedbacks ?? new List<Feedback>();
                        if (_searches.Count == 0 && _requests.Count > 0)
                            _searches = BuildSearchesFromRequests(_requests);
                        if (_prompts.Count == 0)
                            _prompts = BuildDefaultPromptTemplates();
                        return;
                    }
                }
                catch
                {
                    // Fall through to seed
                }
            }
            InitializeWithSampleDataAndSave();
        }
    }

    public IReadOnlyList<User> GetAllUsers()
    {
        lock (_lock)
        {
            EnsureLoaded();
            return _users.ToList();
        }
    }

    public User? GetUserById(Guid id)
    {
        lock (_lock)
        {
            EnsureLoaded();
            return _users.FirstOrDefault(u => u.Id == id);
        }
    }

    public void AddRequest(BenchmarkingRequest request)
    {
        lock (_lock)
        {
            EnsureLoaded();
            _requests.Add(request);
            SaveToFile();
        }
    }

    public IReadOnlyList<BenchmarkingRequest> GetAllRequests()
    {
        lock (_lock)
        {
            EnsureLoaded();
            return _requests.ToList();
        }
    }

    public IReadOnlyList<SavedSearch> GetAllSearches()
    {
        lock (_lock)
        {
            EnsureLoaded();
            return _searches.ToList();
        }
    }

    public SavedSearch? GetSearchById(Guid id)
    {
        lock (_lock)
        {
            EnsureLoaded();
            return _searches.FirstOrDefault(s => s.Id == id);
        }
    }

    public void AddSearch(SavedSearch search)
    {
        lock (_lock)
        {
            EnsureLoaded();
            _searches.Add(search);
            SaveToFile();
        }
    }

    public void UpdateSearch(SavedSearch search)
    {
        lock (_lock)
        {
            EnsureLoaded();
            var idx = _searches.FindIndex(s => s.Id == search.Id);
            if (idx < 0) return;
            _searches[idx] = search;
            SaveToFile();
        }
    }

    public bool RemoveSearch(Guid id)
    {
        lock (_lock)
        {
            EnsureLoaded();
            var idx = _searches.FindIndex(s => s.Id == id);
            if (idx < 0) return false;
            _searches.RemoveAt(idx);
            SaveToFile();
            return true;
        }
    }

    public BenchmarkingRequest? GetRequestById(Guid id)
    {
        lock (_lock)
        {
            EnsureLoaded();
            return _requests.FirstOrDefault(r => r.Id == id);
        }
    }

    public void UpdateRequest(BenchmarkingRequest request)
    {
        lock (_lock)
        {
            EnsureLoaded();
            var idx = _requests.FindIndex(r => r.Id == request.Id);
            if (idx >= 0)
            {
                _requests[idx] = request;
                SaveToFile();
            }
        }
    }

    public bool RemoveRequest(Guid id)
    {
        lock (_lock)
        {
            EnsureLoaded();
            var idx = _requests.FindIndex(r => r.Id == id);
            if (idx < 0) return false;
            _requests.RemoveAt(idx);
            _feedbacks.RemoveAll(f => f.RequestId == id);
            SaveToFile();
            return true;
        }
    }

    public IReadOnlyList<Feedback> GetFeedbackByRequestId(Guid requestId)
    {
        lock (_lock)
        {
            EnsureLoaded();
            return _feedbacks
                .Where(f => f.RequestId == requestId)
                .OrderByDescending(f => f.CreatedAtUtc)
                .ToList();
        }
    }

    public IReadOnlyList<Feedback> GetAllFeedback()
    {
        lock (_lock)
        {
            EnsureLoaded();
            return _feedbacks
                .OrderByDescending(f => f.CreatedAtUtc)
                .ToList();
        }
    }

    public void AddFeedback(Feedback feedback)
    {
        lock (_lock)
        {
            EnsureLoaded();
            _feedbacks.Add(feedback);
            SaveToFile();
        }
    }

    public IReadOnlyList<PromptRecord> GetAllPrompts()
    {
        lock (_lock)
        {
            EnsureLoaded();
            return _prompts.ToList();
        }
    }

    public PromptRecord? GetPromptById(Guid id)
    {
        lock (_lock)
        {
            EnsureLoaded();
            return _prompts.FirstOrDefault(p => p.Id == id);
        }
    }

    public void AddPrompt(PromptRecord prompt)
    {
        lock (_lock)
        {
            EnsureLoaded();
            _prompts.Add(prompt);
            SaveToFile();
        }
    }

    public void UpdatePrompt(PromptRecord prompt)
    {
        lock (_lock)
        {
            EnsureLoaded();
            var idx = _prompts.FindIndex(p => p.Id == prompt.Id);
            if (idx < 0) return;
            _prompts[idx] = prompt;
            SaveToFile();
        }
    }

    public bool RemovePrompt(Guid id)
    {
        lock (_lock)
        {
            EnsureLoaded();
            var idx = _prompts.FindIndex(p => p.Id == id);
            if (idx < 0) return false;
            _prompts.RemoveAt(idx);
            SaveToFile();
            return true;
        }
    }

    /// <summary>
    /// Creates the JSON file with sample data when it doesn't exist (first run).
    /// </summary>
    private void InitializeWithSampleDataAndSave()
    {
        _users = new List<User>
        {
            new User { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Admin User", Email = "admin@example.com", IsAdmin = true },
            new User { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "John Doe", Email = "john@example.com", IsAdmin = false },
            new User { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Jane Smith", Email = "jane@example.com", IsAdmin = false }
        };
        var adminId = _users[0].Id;
        var johnId = _users[1].Id;
        var janeId = _users[2].Id;
        var baseDate = DateTime.UtcNow.AddDays(-10);
        _requests = new List<BenchmarkingRequest>
        {
            new BenchmarkingRequest
            {
                Id = Guid.NewGuid(),
                SearchType = "Standard Search",
                BenchmarkingName = "IT/ITES Set",
                TransactionName = "Provision of IT services",
                Industry = "IT",
                CompanyName = "Acme Corp",
                FinancialYear = "FY 2023-24",
                Purpose = "TP Benchmarking",
                CompanyBusinessDescription = "IT services company.",
                ExclusionKeywords = "excluded",
                AiPrompt = "Analyze IT comparables.",
                Status = "Generated",
                RequestorName = "Admin User",
                RequestorUserId = adminId,
                CreatedAtUtc = baseDate,
                UpdatedAtUtc = baseDate.AddHours(1)
            },
            new BenchmarkingRequest
            {
                Id = Guid.NewGuid(),
                SearchType = "Customized Search",
                BenchmarkingName = "Pharma Set",
                TransactionName = "Pharma R&D",
                Industry = "Pharmaceuticals",
                FinancialYear = "FY 2022-23",
                CompanyBusinessDescription = "Pharma company.",
                ExclusionKeywords = "none",
                AiPrompt = "Analyze pharma comparables.",
                Status = "Submitted",
                RequestorName = "John Doe",
                RequestorUserId = johnId,
                CreatedAtUtc = baseDate.AddDays(1)
            },
            new BenchmarkingRequest
            {
                Id = Guid.NewGuid(),
                SearchType = "Standard Search",
                BenchmarkingName = "Manufacturing Set",
                TransactionName = "Manufacturing services",
                Industry = "Manufacturing",
                FinancialYear = "FY 2023-24",
                CompanyBusinessDescription = "Manufacturing company.",
                ExclusionKeywords = "exclude",
                AiPrompt = "Analyze manufacturing comparables.",
                Status = "InProcess",
                RequestorName = "Jane Smith",
                RequestorUserId = janeId,
                CreatedAtUtc = baseDate.AddDays(2)
            }
        };
        _searches = BuildSearchesFromRequests(_requests);
        _prompts = BuildDefaultPromptTemplates();
        _feedbacks = new List<Feedback>();
        SaveToFile();
    }

    private static List<SavedSearch> BuildSearchesFromRequests(IEnumerable<BenchmarkingRequest> requests)
    {
        return requests
            .Where(r =>
                string.Equals(r.SearchType, "Standard Search", StringComparison.OrdinalIgnoreCase)
                || string.Equals(r.SearchType, "Customized Search", StringComparison.OrdinalIgnoreCase)
                || string.Equals(r.SearchType, "Customised Search", StringComparison.OrdinalIgnoreCase))
            .Select(r => new SavedSearch
            {
                Id = Guid.NewGuid(),
                Name = r.BenchmarkingName,
                SearchType = string.Equals(r.SearchType, "Standard Search", StringComparison.OrdinalIgnoreCase)
                    ? "Standard Search"
                    : "Customized Search",
                FinancialYear = r.FinancialYear,
                RequestorUserId = r.RequestorUserId,
                RequestorName = r.RequestorName,
                IsAdminManaged = string.Equals(r.SearchType, "Standard Search", StringComparison.OrdinalIgnoreCase),
                TransactionName = r.TransactionName,
                Industry = r.Industry,
                CompanyName = r.CompanyName,
                Purpose = r.Purpose,
                CompanyBusinessDescription = r.CompanyBusinessDescription,
                ExclusionKeywords = r.ExclusionKeywords,
                AiPrompt = r.AiPrompt,
                BenchmarkingRequestId = r.Id,
                CreatedAtUtc = r.CreatedAtUtc,
                UpdatedAtUtc = r.UpdatedAtUtc
            })
            .ToList();
    }

    private static List<PromptRecord> BuildDefaultPromptTemplates()
    {
        return new List<PromptRecord>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Default (v1)",
                Description = "Structured prompt built from business description + exclusions.",
                PromptText = "Auto-generated",
                IsDefault = true,
                IsManagedTemplate = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "TP Focused",
                Description = "Emphasize tested party profile and key differentiators.",
                PromptText = "Focus on tested party description. Highlight products/services, key functions, risks, and business model. Apply exclusions strictly.",
                IsDefault = false,
                IsManagedTemplate = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Strict exclusions",
                Description = "Explicitly call out exclusion handling.",
                PromptText = "Use tested party description as primary context. Treat exclusions as hard filters. Do not include excluded words in any reasoning output.",
                IsDefault = false,
                IsManagedTemplate = true,
                CreatedAtUtc = DateTime.UtcNow
            }
        };
    }

    private void SaveToFile()
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        var model = new DummyDataFileModel { Users = _users, Requests = _requests, Searches = _searches, Prompts = _prompts, Feedbacks = _feedbacks };
        var json = JsonSerializer.Serialize(model, JsonOptions);
        File.WriteAllText(_filePath, json);
    }
}
