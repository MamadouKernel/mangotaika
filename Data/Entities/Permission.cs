using Microsoft.AspNetCore.Identity;

namespace MangoTaika.Data.Entities;

public class Permission
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Libelle { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}

public class RolePermission
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public IdentityRole<Guid> Role { get; set; } = null!;
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
}
