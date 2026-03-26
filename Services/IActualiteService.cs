using MangoTaika.DTOs;

namespace MangoTaika.Services;

public interface IActualiteService
{
    Task<List<ActualiteDto>> GetAllPublishedAsync();
    Task<List<ActualiteDto>> GetAllAsync();
    Task<ActualiteDto?> GetByIdAsync(Guid id);
    Task<ActualiteDto> CreateAsync(ActualiteCreateDto dto, Guid createurId, string? imagePath);
    Task<bool> UpdateAsync(Guid id, ActualiteCreateDto dto, string? imagePath);
    Task<bool> PublierAsync(Guid id);
    Task<bool> DepublierAsync(Guid id);
    Task<bool> DeleteAsync(Guid id);
}
