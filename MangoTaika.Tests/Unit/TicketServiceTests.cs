using FluentAssertions;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using MangoTaika.Hubs;
using MangoTaika.Services;
using MangoTaika.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MangoTaika.Tests.Unit;

public sealed class TicketServiceTests
{
    [Fact]
    public async Task CreateAsync_AssignsLeastLoadedActiveAgent_WhenDefaultAgentIsInactive()
    {
        await using var db = TestDbContextFactory.CreateDbContext();
        await TestDataSeeder.EnsureRolesAsync(db, "AgentSupport");

        var supportGroup = new Groupe { Id = Guid.NewGuid(), Nom = "Support L1" };
        db.Groupes.Add(supportGroup);
        await db.SaveChangesAsync();

        var creator = await TestDataSeeder.AddUserAsync(db, "Awa", "Demandeur", []);
        var inactiveDefault = await TestDataSeeder.AddUserAsync(db, "Inactif", "Support", ["AgentSupport"], isActive: false, groupeId: supportGroup.Id);
        var busyAgent = await TestDataSeeder.AddUserAsync(db, "Charge", "Support", ["AgentSupport"], groupeId: supportGroup.Id);
        var availableAgent = await TestDataSeeder.AddUserAsync(db, "Libre", "Support", ["AgentSupport"], groupeId: supportGroup.Id);

        db.SupportCatalogueServices.Add(new SupportServiceCatalogueItem
        {
            Id = Guid.NewGuid(),
            Code = "SRV-L1",
            Nom = "Support poste",
            Description = "Support utilisateur",
            EstActif = true,
            AssigneParDefautId = inactiveDefault.Id,
            GroupeParDefautId = supportGroup.Id,
            DelaiSlaHeures = 8
        });

        db.Tickets.AddRange(
            CreateTicket(creator.Id, busyAgent.Id, "INC-BUSY-1", StatutTicket.Affecte, DateTime.UtcNow.AddHours(6)),
            CreateTicket(creator.Id, busyAgent.Id, "INC-BUSY-2", StatutTicket.EnCours, DateTime.UtcNow.AddHours(8)));
        await db.SaveChangesAsync();

        var serviceItem = await db.SupportCatalogueServices.SingleAsync();
        var service = new TicketService(db, new TestHubContext<NotificationHub>());

        var created = await service.CreateAsync(new TicketCreateDto
        {
            ServiceCatalogueId = serviceItem.Id,
            Sujet = "Incident poste",
            Description = "Le poste ne demarre plus."
        }, creator.Id);

        created.AssigneAId.Should().Be(availableAgent.Id);
        created.GroupeAssigneId.Should().Be(supportGroup.Id);
        created.Statut.Should().Be(StatutTicket.Affecte);
    }

    [Fact]
    public async Task GetByIdAsync_EscalatesOverdueTicket_AndPersistsNotifications()
    {
        await using var db = TestDbContextFactory.CreateDbContext();
        await TestDataSeeder.EnsureRolesAsync(db, "AgentSupport", "Superviseur");

        var creator = await TestDataSeeder.AddUserAsync(db, "Mariam", "Scout", []);
        var agent = await TestDataSeeder.AddUserAsync(db, "Nadia", "Agent", ["AgentSupport"]);
        var superviseur = await TestDataSeeder.AddUserAsync(db, "Yao", "Superviseur", ["Superviseur"]);

        var overdueTicket = CreateTicket(
            creator.Id,
            agent.Id,
            "INC-OVERDUE-1",
            StatutTicket.Affecte,
            DateTime.UtcNow.AddMinutes(-30));
        overdueTicket.Sujet = "SLA critique";
        overdueTicket.Description = "Le SLA est deja depasse.";

        db.Tickets.Add(overdueTicket);
        await db.SaveChangesAsync();

        var hubContext = new TestHubContext<NotificationHub>();
        var service = new TicketService(db, hubContext);

        var dto = await service.GetByIdAsync(overdueTicket.Id);

        dto.Should().NotBeNull();
        dto!.EstEscalade.Should().BeTrue();
        dto.NiveauEscalade.Should().Be(2);

        var persistedTicket = await db.Tickets.SingleAsync(t => t.Id == overdueTicket.Id);
        persistedTicket.EstEscalade.Should().BeTrue();
        persistedTicket.NiveauEscalade.Should().Be(2);

        var notifications = await db.NotificationsUtilisateur
            .Where(n => n.Message.Contains(overdueTicket.NumeroTicket))
            .ToListAsync();
        notifications.Select(n => n.UserId).Should().Contain([agent.Id, superviseur.Id]);
        hubContext.TypedClients.SentMessages.Should().Contain(m => m.Method == "RecevoirNotification");
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsUserTickets_WhenEscalationRulesRun()
    {
        await using var db = TestDbContextFactory.CreateDbContext();
        await TestDataSeeder.EnsureRolesAsync(db, "AgentSupport");

        var creator = await TestDataSeeder.AddUserAsync(db, "Koffi", "Parent", []);
        var agent = await TestDataSeeder.AddUserAsync(db, "Sarah", "Agent", ["AgentSupport"]);

        var ticket = CreateTicket(creator.Id, agent.Id, "INC-MINE-1", StatutTicket.Affecte, DateTime.UtcNow.AddHours(2));
        ticket.Sujet = "Mes tickets";
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var service = new TicketService(db, new TestHubContext<NotificationHub>());

        var results = await service.GetByUserAsync(creator.Id);

        results.Should().ContainSingle(t => t.Id == ticket.Id);
    }

    private static Ticket CreateTicket(Guid creatorId, Guid? assigneeId, string number, StatutTicket statut, DateTime slaDeadline)
    {
        return new Ticket
        {
            Id = Guid.NewGuid(),
            NumeroTicket = number,
            Sujet = number,
            Description = "Ticket de test",
            CreateurId = creatorId,
            AssigneAId = assigneeId,
            Statut = statut,
            Type = TypeTicket.Incident,
            Categorie = CategorieTicket.Technique,
            Impact = ImpactTicket.Moyen,
            Urgence = UrgenceTicket.Haute,
            Priorite = PrioriteTicket.Haute,
            DateCreation = DateTime.UtcNow.AddHours(-1),
            DateLimiteSla = slaDeadline
        };
    }
}
