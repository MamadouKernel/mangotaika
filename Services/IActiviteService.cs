using MangoTaika.Data.Entities;
using MangoTaika.DTOs;

namespace MangoTaika.Services;

public interface IActiviteService
{
    Task<List<ActiviteDto>> GetAllAsync();
    Task<ActiviteDto?> GetByIdAsync(Guid id);
    Task<ActiviteDto> CreateAsync(ActiviteCreateDto dto, Guid createurId);
    Task<bool> UpdateAsync(Guid id, ActiviteCreateDto dto);
    Task<bool> UpdateStatutAsync(Guid id, StatutActivite statut);
    Task<bool> DeleteAsync(Guid id);
}
