namespace MangoTaika.DTOs;

public class ActualiteDto
{
    public Guid Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string Contenu { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Resume { get; set; }
    public DateTime DatePublication { get; set; }
    public bool EstPublie { get; set; }
    public string? NomCreateur { get; set; }
}

public class ActualiteCreateDto
{
    public string Titre { get; set; } = string.Empty;
    public string Contenu { get; set; } = string.Empty;
    public string? Resume { get; set; }
    public Microsoft.AspNetCore.Http.IFormFile? Image { get; set; }
}
