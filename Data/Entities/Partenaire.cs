namespace MangoTaika.Data.Entities;

public class Partenaire
{
    public Guid Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? SiteWeb { get; set; }
    public string? TypePartenariat { get; set; }
    public bool EstActif { get; set; } = true;
    public bool EstSupprime { get; set; } = false;
    public int Ordre { get; set; } = 0;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
}

public class LienReseauSocial
{
    public Guid Id { get; set; }
    public string Plateforme { get; set; } = string.Empty; // Facebook, Instagram, Twitter, YouTube, TikTok, WhatsApp
    public string Url { get; set; } = string.Empty;
    public string? Icone { get; set; }
    public bool EstActif { get; set; } = true;
    public int Ordre { get; set; } = 0;
}
