using MangoTaika.Data.Entities;

namespace MangoTaika.Models;

public class MessagesAdminViewModel
{
    public List<ContactMessage> Messages { get; set; } = [];
    public List<LivreDor> LivreDorMessages { get; set; } = [];
    public string? TypeFiltre { get; set; }
}
