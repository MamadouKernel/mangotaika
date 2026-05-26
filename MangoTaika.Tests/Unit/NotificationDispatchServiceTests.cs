using FluentAssertions;
using MangoTaika.Data.Entities;
using MangoTaika.Hubs;
using MangoTaika.Services;
using MangoTaika.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MangoTaika.Tests.Unit;

public sealed class NotificationDispatchServiceTests
{
    [Fact]
    public async Task SendAsync_PersistsAndPushesNotifications()
    {
        await using var db = TestDbContextFactory.CreateDbContext();
        await TestDataSeeder.EnsureRolesAsync(db, "Scout");
        var user = await TestDataSeeder.AddUserAsync(db, "Lms", "Notif", ["Scout"]);
        var hub = new TestHubContext<NotificationHub>();
        var emailService = new TestEmailNotificationService();
        var smsService = new TestSmsService();
        var configuration = new ConfigurationBuilder().Build();
        var service = new NotificationDispatchService(
            db,
            hub,
            emailService,
            smsService,
            configuration,
            NullLogger<NotificationDispatchService>.Instance);

        await service.SendAsync([user.Id], "Annonce", "Nouvelle annonce LMS", "LMS", "/Formations/Suivre/1");

        var notification = await db.NotificationsUtilisateur.SingleAsync();
        notification.UserId.Should().Be(user.Id);
        notification.Categorie.Should().Be("LMS");
        notification.Titre.Should().Be("Annonce");
        hub.TypedClients.SentMessages.Should().Contain(m => m.Method == "RecevoirNotification");
        emailService.SentMessages.Should().ContainSingle(m => m.Email == user.Email && m.Title == "Annonce");
    }

    private sealed class TestEmailNotificationService : IEmailNotificationService
    {
        public List<(string Email, string Title)> SentMessages { get; } = [];

        public Task SendAsync(
            string toEmail,
            string subject,
            string message,
            string? recipientName = null,
            string? category = null,
            string? actionUrl = null)
        {
            SentMessages.Add((toEmail, subject));
            return Task.CompletedTask;
        }
    }

    private sealed class TestSmsService : ISmsService
    {
        public Task SendSmsAsync(string phoneNumber, string message)
            => Task.CompletedTask;
    }
}
