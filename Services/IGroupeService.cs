using MangoTaika.DTOs;

namespace MangoTaika.Services;

public interface IGroupeService
{
    Task<List<GroupeDto>> GetAllAsync();
    Task<GroupeDto?> GetByIdAsync(Guid id);
    Task<GroupeDto> CreateAsync(GroupeCreateDto dto);
    Task<bool> UpdateAsync(Guid id, GroupeCreateDto dto);
    Task<bool> DeleteAsync(Guid id);
}
