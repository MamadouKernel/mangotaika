using MangoTaika.Data;
using MangoTaika.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace MangoTaika.Tests.Infrastructure;

public static class TestDataSeeder
{
    public static async Task EnsureRolesAsync(AppDbContext db, params string[] roleNames)
    {
        foreach (var roleName in roleNames.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var normalized = roleName.ToUpperInvariant();
            var role = db.Roles.FirstOrDefault(r => r.NormalizedName == normalized);
            if (role is null)
            {
                db.Roles.Add(new IdentityRole<Guid>
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    NormalizedName = normalized
                });
            }
        }

        await db.SaveChangesAsync();
    }

    public static async Task<ApplicationUser> AddUserAsync(
        AppDbContext db,
        string prenom,
        string nom,
        IEnumerable<string> roles,
        bool isActive = true,
        Guid? groupeId = null)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"{prenom}.{nom}.{Guid.NewGuid():N}@test.local",
            Email = $"{prenom}.{nom}.{Guid.NewGuid():N}@test.local",
            PhoneNumber = Guid.NewGuid().ToString("N")[..10],
            Prenom = prenom,
            Nom = nom,
            IsActive = isActive,
            GroupeId = groupeId,
            EmailConfirmed = true,
            PhoneNumberConfirmed = true
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        foreach (var roleName in roles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var role = db.Roles.First(r => r.NormalizedName == roleName.ToUpperInvariant());
            db.UserRoles.Add(new IdentityUserRole<Guid>
            {
                UserId = user.Id,
                RoleId = role.Id
            });
        }

        await db.SaveChangesAsync();
        return user;
    }
}
