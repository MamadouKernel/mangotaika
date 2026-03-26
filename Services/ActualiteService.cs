using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Services;

public class ActualiteService(AppDbContext db) : IActualiteService
{
    public async Task<List<ActualiteDto>> GetAllPublishedAsync()
    {
        return await db.Actualites
            .Include(a => a.Createur)
            .Where(a => !a.EstSupprime && a.EstPublie)
            .OrderByDescending(a => a.DatePublication)
            .Select(a => ToDto(a))
            .ToListAsync();
    }

    public async Task<List<ActualiteDto>> GetAllAsync()
    {
        return await db.Actualites
            .Include(a => a.Createur)
            .Where(a => !a.EstSupprime)
            .OrderByDescending(a => a.DateCreation)
            .Select(a => ToDto(a))
            .ToListAsync();
    }

    public async Task<ActualiteDto?> GetByIdAsync(Guid id)
    {
        var a = await db.Actualites.Include(x => x.Createur)
            .FirstOrDefaultAsync(x => x.Id == id && !x.EstSupprime);
        return a is null ? null : ToDto(a);
    }

    public async Task<ActualiteDto> CreateAsync(ActualiteCreateDto dto, Guid createurId, string? imagePath)
    {
        var actualite = new Actualite
        {
            Id = Guid.NewGuid(),
            Titre = dto.Titre,
            Contenu = dto.Contenu,
            Resume = dto.Resume,
            ImageUrl = imagePath,
            CreateurId = createurId
        };
        db.Actualites.Add(actualite);
        await db.SaveChangesAsync();
        return ToDto(actualite);
    }

    public async Task<bool> UpdateAsync(Guid id, ActualiteCreateDto dto, string? imagePath)
    {
        var a = await db.Actualites.FindAsync(id);
        if (a is null || a.EstSupprime) return false;
        a.Titre = dto.Titre;
        a.Contenu = dto.Contenu;
        a.Resume = dto.Resume;
        if (imagePath != null) a.ImageUrl = imagePath;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> PublierAsync(Guid id)
    {
        var a = await db.Actualites.FindAsync(id);
        if (a is null) return false;
        a.EstPublie = true;
        a.DatePublication = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DepublierAsync(Guid id)
    {
        var a = await db.Actualites.FindAsync(id);
        if (a is null) return false;
        a.EstPublie = false;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var a = await db.Actualites.FindAsync(id);
        if (a is null) return false;
        a.EstSupprime = true;
        await db.SaveChangesAsync();
        return true;
    }

    private static ActualiteDto ToDto(Actualite a) => new()
    {
        Id = a.Id,
        Titre = a.Titre,
        Contenu = a.Contenu,
        ImageUrl = a.ImageUrl,
        Resume = a.Resume,
        DatePublication = a.DatePublication,
        EstPublie = a.EstPublie,
        NomCreateur = a.Createur != null ? $"{a.Createur.Prenom} {a.Createur.Nom}" : null
    };
}
