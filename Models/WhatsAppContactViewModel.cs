namespace MangoTaika.Models;

public class WhatsAppContactViewModel
{
    public string DisplayPhoneNumber { get; set; } = string.Empty;
    public List<WhatsAppContactOptionViewModel> Options { get; set; } = [];
}

public class WhatsAppContactOptionViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string IconClass { get; set; } = "bi-chat-dots";
}
