using MangoTaika.DTOs;
using System.Security.Claims;

namespace MangoTaika.Services;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync(ClaimsPrincipal user);
}
