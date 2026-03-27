using Microsoft.AspNetCore.Mvc;

namespace MangoTaika.Helpers;

public static class ControllerFeedbackExtensions
{
    public static void AddDomainError(this Controller controller, Exception exception)
    {
        controller.ModelState.AddModelError(string.Empty, exception.Message);
    }

    public static void SetDomainError(this Controller controller, Exception exception)
    {
        controller.TempData["Error"] = exception.Message;
    }
}
