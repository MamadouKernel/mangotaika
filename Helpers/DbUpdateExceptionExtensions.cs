using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Helpers;

public static class DbUpdateExceptionExtensions
{
    public static bool ReferencesConstraint(this DbUpdateException exception, string constraintName)
    {
        var message = exception.Message;
        var innerMessage = exception.InnerException?.Message;

        return message.Contains(constraintName, StringComparison.OrdinalIgnoreCase)
            || (innerMessage?.Contains(constraintName, StringComparison.OrdinalIgnoreCase) ?? false);
    }
}
