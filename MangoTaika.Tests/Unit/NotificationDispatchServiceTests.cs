using FluentAssertions;
using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Hubs;
using MangoTaika.Services;
using MangoTaika.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MangoTaika.Tests.Unit;

public sealed class NotificationDispatchServiceTests
{
    [Fact]
    public async Task SendAsync_PersistsAndPushesNotifications()
    {
        await using var db = CreateDbContext();
        await TestDataSeeder.EnsureRolesAsync(db, "Scout");
        var user = await TestDataSeeder.AddUserAsync(db, "Lms", "Notif", ["Scout"]);
        var hub = new TestHubContext<NotificationHub>();
        var service = new NotificationDispatchService(db, hub);

        await service.SendAsync([user.Id], "Annonce", "Nouvelle annonce LMS", "LMS", "/Formations/Suivre/1");

        var notification = await db.NotificationsUtilisateur.SingleAsync();
        notification.UserId.Should().Be(user.Id);
        notification.Categorie.Should().Be("LMS");
        notification.Titre.Should().Be("Annonce");
        hub.TypedClients.SentMessages.Should().Contain(m => m.Method == "RecevoirNotification");
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
