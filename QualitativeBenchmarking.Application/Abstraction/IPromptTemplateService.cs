using KPMG.QualitativeBenchmarking.Application.Dtos.Ai;

namespace KPMG.QualitativeBenchmarking.Application.Abstraction;

public interface IPromptTemplateService
{
    Task<IReadOnlyList<PromptTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default);
    Task<PromptTemplateDto> CreateTemplateAsync(CreatePromptTemplateDto dto, Guid? createdByUserId, string createdByUserName, CancellationToken cancellationToken = default);
    Task<PromptTemplateDto?> UpdateTemplateAsync(Guid id, UpdatePromptTemplateDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteTemplateAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveGeneratedPromptAsync(string prompt, string businessDescription, string exclusionKeywords, Guid? createdByUserId, string createdByUserName, CancellationToken cancellationToken = default);
}

