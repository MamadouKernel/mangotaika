using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Services;

public class ActiviteService(AppDbContext db) : IActiviteService
{
    public async Task<List<ActiviteDto>> GetAllAsync()
    {
        return await db.Activites
            .Include(a => a.Groupe)
            .Include(a => a.Createur)
            .Include(a => a.Participants)
            .Include(a => a.Documents)
            .Where(a => !a.EstSupprime)
            .OrderByDescending(a => a.DateCreation)
            .Select(a => new ActiviteDto
            {
                Id = a.Id,
                Titre = a.Titre,
                Description = a.Description,
                Type = a.Type,
                DateDebut = a.DateDebut,
                DateFin = a.DateFin,
                Lieu = a.Lieu,
                BudgetPrevisionnel = a.BudgetPrevisionnel,
                NomResponsable = a.NomResponsable,
                Statut = a.Statut,
                DateCloturePointage = a.DateCloturePointage,
                NomGroupe = a.Groupe != null ? a.Groupe.Nom : null,
                GroupeId = a.GroupeId,
                NomCreateur = a.Createur.Prenom + " " + a.Createur.Nom,
                DateCreation = a.DateCreation,
                NbParticipants = a.Participants.Count,
                NbDocuments = a.Documents.Count
            })
            .ToListAsync();
    }

    public async Task<ActiviteDto?> GetByIdAsync(Guid id)
    {
        var a = await db.Activites
            .Include(x => x.Groupe)
            .Include(x => x.Createur)
            .Include(x => x.Documents)
            .Include(x => x.Participants).ThenInclude(p => p.Scout).ThenInclude(s => s.Branche)
            .Include(x => x.Commentaires).ThenInclude(c => c.Auteur)
            .FirstOrDefaultAsync(x => x.Id == id && !x.EstSupprime);
        if (a is null) return null;

        return new ActiviteDto
        {
            Id = a.Id,
            Titre = a.Titre,
            Description = a.Description,
            Type = a.Type,
            DateDebut = a.DateDebut,
            DateFin = a.DateFin,
            Lieu = a.Lieu,
            BudgetPrevisionnel = a.BudgetPrevisionnel,
            NomResponsable = a.NomResponsable,
            Statut = a.Statut,
            MotifRejet = a.MotifRejet,
            DateCloturePointage = a.DateCloturePointage,
            NomGroupe = a.Groupe?.Nom,
            GroupeId = a.GroupeId,
            NomCreateur = $"{a.Createur.Prenom} {a.Createur.Nom}",
            DateCreation = a.DateCreation,
            NbParticipants = a.Participants.Count,
            NbDocuments = a.Documents.Count,
            Documents = a.Documents.Select(d => new DocumentActiviteDto
            {
                Id = d.Id,
                NomFichier = d.NomFichier,
                CheminFichier = d.CheminFichier,
                TypeDocument = d.TypeDocument,
                DateUpload = d.DateUpload
            }).OrderByDescending(d => d.DateUpload).ToList(),
            Participants = a.Participants.Select(p => new ParticipantActiviteDto
            {
                Id = p.Id,
                ScoutId = p.ScoutId,
                NomComplet = $"{p.Scout.Prenom} {p.Scout.Nom}",
                Matricule = p.Scout.Matricule,
                NomBranche = p.Scout.Branche?.Nom,
                Presence = p.Presence
            }).OrderBy(p => p.NomComplet).ToList(),
            Commentaires = a.Commentaires.Select(c => new CommentaireActiviteDto
            {
                Id = c.Id,
                NomAuteur = $"{c.Auteur.Prenom} {c.Auteur.Nom}",
                Contenu = c.Contenu,
                TypeAction = c.TypeAction,
                DateCreation = c.DateCreation
            }).OrderByDescending(c => c.DateCreation).ToList()
        };
    }

    public async Task<ActiviteDto> CreateAsync(ActiviteCreateDto dto, Guid createurId)
    {
        var activite = new Activite
        {
            Id = Guid.NewGuid(),
            Titre = dto.Titre,
            Description = dto.Description,
            Type = dto.Type,
            DateDebut = dto.DateDebut,
            DateFin = dto.DateFin,
            Lieu = dto.Lieu,
            BudgetPrevisionnel = dto.BudgetPrevisionnel,
            NomResponsable = dto.NomResponsable,
            GroupeId = dto.GroupeId,
            CreateurId = createurId
        };
        db.Activites.Add(activite);

        // Journal
        db.CommentairesActivite.Add(new CommentaireActivite
        {
            Id = Guid.NewGuid(),
            ActiviteId = activite.Id,
            AuteurId = createurId,
            Contenu = "ActivitÃ© crÃ©Ã©e.",
            TypeAction = "CrÃ©ation"
        });

        await db.SaveChangesAsync();
        return (await GetByIdAsync(activite.Id))!;
    }

    public async Task<bool> UpdateAsync(Guid id, ActiviteCreateDto dto)
    {
        var activite = await db.Activites.FindAsync(id);
        if (activite is null) return false;
        activite.Titre = dto.Titre;
        activite.Description = dto.Description;
        activite.Type = dto.Type;
        activite.DateDebut = dto.DateDebut;
        activite.DateFin = dto.DateFin;
        activite.Lieu = dto.Lieu;
        activite.BudgetPrevisionnel = dto.BudgetPrevisionnel;
        activite.NomResponsable = dto.NomResponsable;
        activite.GroupeId = dto.GroupeId;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateStatutAsync(Guid id, StatutActivite statut)
    {
        var activite = await db.Activites.FindAsync(id);
        if (activite is null) return false;
        activite.Statut = statut;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var activite = await db.Activites.FindAsync(id);
        if (activite is null) return false;
        activite.EstSupprime = true;
        await db.SaveChangesAsync();
        return true;
    }
}

