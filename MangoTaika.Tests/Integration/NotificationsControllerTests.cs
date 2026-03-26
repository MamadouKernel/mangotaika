using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using MangoTaika.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MangoTaika.Tests.Integration;

public sealed class NotificationsControllerTests
{
    [Fact]
    public async Task Mine_ReturnsOnlyNotificationsForCurrentUser()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser currentUser = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "AgentSupport");
            currentUser = await TestDataSeeder.AddUserAsync(db, "Jean", "Support", ["AgentSupport"]);
            var otherUser = await TestDataSeeder.AddUserAsync(db, "Autre", "Support", ["AgentSupport"]);

            db.NotificationsUtilisateur.AddRange(
                new NotificationUtilisateur
                {
                    Id = Guid.NewGuid(),
                    UserId = currentUser.Id,
                    Titre = "Escalade",
                    Message = "Notification courante",
                    Categorie = "Support"
                },
                new NotificationUtilisateur
                {
                    Id = Guid.NewGuid(),
                    UserId = otherUser.Id,
                    Titre = "Escalade",
                    Message = "Notification autre user",
                    Categorie = "Support"
                });
        });

        using var client = factory.CreateAuthenticatedClient(currentUser.Id, "AgentSupport");

        var response = await client.GetAsync("/Notifications/Mine?take=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<NotificationDto>>();
        items.Should().NotBeNull();
        items!.Should().ContainSingle();
        items[0].Message.Should().Be("Notification courante");
    }

    [Fact]
    public async Task MarkAllRead_MarksOnlyCurrentUserNotifications()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser currentUser = null!;
        ApplicationUser otherUser = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "AgentSupport");
            currentUser = await TestDataSeeder.AddUserAsync(db, "Aminata", "Support", ["AgentSupport"]);
            otherUser = await TestDataSeeder.AddUserAsync(db, "Bamba", "Support", ["AgentSupport"]);

            db.NotificationsUtilisateur.AddRange(
                new NotificationUtilisateur
                {
                    Id = Guid.NewGuid(),
                    UserId = currentUser.Id,
                    Titre = "Escalade",
                    Message = "A lire",
                    Categorie = "Support"
                },
                new NotificationUtilisateur
                {
                    Id = Guid.NewGuid(),
                    UserId = otherUser.Id,
                    Titre = "Escalade",
                    Message = "Ne pas modifier",
                    Categorie = "Support"
                });
        });

        using var client = factory.CreateAuthenticatedClient(currentUser.Id, "AgentSupport");
        var dashboardHtml = await client.GetStringAsync("/Dashboard");
        var token = HtmlTestHelpers.ExtractAntiForgeryToken(dashboardHtml);

        var request = new HttpRequestMessage(HttpMethod.Post, "/Notifications/MarkAllRead");
        request.Headers.Add("RequestVerificationToken", token);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MangoTaika.Data.AppDbContext>();
        var currentNotifications = db.NotificationsUtilisateur.Where(n => n.UserId == currentUser.Id).ToList();
        var otherNotifications = db.NotificationsUtilisateur.Where(n => n.UserId == otherUser.Id).ToList();

        currentNotifications.Should().OnlyContain(n => n.EstLue);
        otherNotifications.Should().OnlyContain(n => !n.EstLue);
    }
}
