using MangoTaika.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MangoTaika.Tests.Infrastructure;

public static class TestDbContextFactory
{
    public static AppDbContext CreateDbContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
