using KPMG.QualitativeBenchmarking.Application.Abstraction;
using KPMG.QualitativeBenchmarking.Application.Dtos.Ai;
using KPMG.QualitativeBenchmarking.Domain.Entities;
using KPMG.QualitativeBenchmarking.Infrastructure.Data;

namespace KPMG.QualitativeBenchmarking.Infrastructure.Services;

public class PromptTemplateService : IPromptTemplateService
{
    private readonly DummyDataStore _store;

    public PromptTemplateService(DummyDataStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public Task<IReadOnlyList<PromptTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var list = _store.GetAllPrompts()
            .Where(p => p.IsManagedTemplate)
            .OrderByDescending(p => p.IsDefault)
            .ThenBy(p => p.Name)
            .Select(MapTemplate)
            .ToList();
        return Task.FromResult<IReadOnlyList<PromptTemplateDto>>(list);
    }

    public Task<PromptTemplateDto> CreateTemplateAsync(CreatePromptTemplateDto dto, Guid? createdByUserId, string createdByUserName, CancellationToken cancellationToken = default)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.Name)) throw new ArgumentException("Name is required.", nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.PromptText)) throw new ArgumentException("PromptText is required.", nameof(dto));

        var entity = new PromptRecord
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Description = dto.Description,
            PromptText = dto.PromptText,
            IsDefault = dto.IsDefault,
            IsManagedTemplate = true,
            CreatedByUserId = createdByUserId,
            CreatedByUserName = string.IsNullOrWhiteSpace(createdByUserName) ? "Admin" : createdByUserName.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
        _store.AddPrompt(entity);
        return Task.FromResult(MapTemplate(entity));
    }

    public Task<PromptTemplateDto?> UpdateTemplateAsync(Guid id, UpdatePromptTemplateDto dto, CancellationToken cancellationToken = default)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));
        var entity = _store.GetPromptById(id);
        if (entity == null || !entity.IsManagedTemplate) return Task.FromResult<PromptTemplateDto?>(null);

        if (dto.Name != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) throw new ArgumentException("Name cannot be empty.", nameof(dto));
            entity.Name = dto.Name.Trim();
        }
        if (dto.Description != null) entity.Description = dto.Description;
        if (dto.PromptText != null)
        {
            if (string.IsNullOrWhiteSpace(dto.PromptText)) throw new ArgumentException("PromptText cannot be empty.", nameof(dto));
            entity.PromptText = dto.PromptText;
        }
        if (dto.IsDefault.HasValue) entity.IsDefault = dto.IsDefault.Value;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        _store.UpdatePrompt(entity);
        return Task.FromResult<PromptTemplateDto?>(MapTemplate(entity));
    }

    public Task<bool> DeleteTemplateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = _store.GetPromptById(id);
        if (entity == null || !entity.IsManagedTemplate) return Task.FromResult(false);
        return Task.FromResult(_store.RemovePrompt(id));
    }

    public Task SaveGeneratedPromptAsync(string prompt, string businessDescription, string exclusionKeywords, Guid? createdByUserId, string createdByUserName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt)) return Task.CompletedTask;
        var entity = new PromptRecord
        {
            Id = Guid.NewGuid(),
            Name = "AI Generated Prompt",
            PromptText = prompt,
            IsDefault = false,
            IsManagedTemplate = false,
            BusinessDescription = businessDescription,
            ExclusionKeywords = exclusionKeywords,
            CreatedByUserId = createdByUserId,
            CreatedByUserName = string.IsNullOrWhiteSpace(createdByUserName) ? "User" : createdByUserName.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
        _store.AddPrompt(entity);
        return Task.CompletedTask;
    }

    private static PromptTemplateDto MapTemplate(PromptRecord p)
    {
        return new PromptTemplateDto
        {
            Id = p.Id,
            Name = p.Name ?? "",
            Description = p.Description,
            PromptText = p.PromptText,
            IsDefault = p.IsDefault
        };
    }
}

