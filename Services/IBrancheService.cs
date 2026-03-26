using MangoTaika.DTOs;

namespace MangoTaika.Services;

public interface IBrancheService
{
    Task<List<BrancheDto>> GetAllAsync();
    Task<BrancheDto?> GetByIdAsync(Guid id);
    Task<BrancheDto> CreateAsync(BrancheCreateDto dto);
    Task<bool> UpdateAsync(Guid id, BrancheCreateDto dto);
    Task<bool> DeleteAsync(Guid id);
}
