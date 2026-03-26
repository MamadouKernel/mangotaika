using MangoTaika.DTOs;

namespace MangoTaika.Services;

public interface IScoutService
{
    Task<List<ScoutDto>> GetAllAsync();
    Task<ScoutDto?> GetByIdAsync(Guid id);
    Task<ScoutDto> CreateAsync(ScoutCreateDto dto);
    Task<bool> UpdateAsync(Guid id, ScoutCreateDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<List<ScoutDto>> SearchAsync(string terme);
    Task<ScoutImportResultDto> ImportFromExcelAsync(Stream fileStream);
    byte[] GenerateImportTemplate();
}
