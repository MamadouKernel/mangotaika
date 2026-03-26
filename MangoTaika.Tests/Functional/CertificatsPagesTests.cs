using System.Net;
using FluentAssertions;
using MangoTaika.Data.Entities;
using MangoTaika.Tests.Infrastructure;
using Xunit;

namespace MangoTaika.Tests.Functional;

public sealed class CertificatsPagesTests
{
    [Fact]
    public async Task MesCertificats_PageLoadsForScout()
    {
        await using var factory = new SupportWebApplicationFactory();
        ApplicationUser scoutUser = null!;

        await factory.SeedAsync(async db =>
        {
            await TestDataSeeder.EnsureRolesAsync(db, "Scout");
            scoutUser = await TestDataSeeder.AddUserAsync(db, "Mariam", "Scout", ["Scout"]);
            var author = await TestDataSeeder.AddUserAsync(db, "Coach", "Form", []);

            var scout = new Scout
            {
                Id = Guid.NewGuid(),
                UserId = scoutUser.Id,
                Matricule = "SC-CERT-001",
                Prenom = "Mariam",
                Nom = "Scout",
                DateNaissance = new DateTime(2010, 2, 15),
                IsActive = true
            };
            var formation = new Formation
            {
                Id = Guid.NewGuid(),
                AuteurId = author.Id,
                Titre = "Orientation scoute",
                Statut = StatutFormation.Publiee,
                DelivreBadge = true,
                DelivreAttestation = true
            };

            db.Scouts.Add(scout);
            db.Formations.Add(formation);
            db.CertificationsFormation.Add(new CertificationFormation
            {
                Id = Guid.NewGuid(),
                ScoutId = scout.Id,
                FormationId = formation.Id,
                Type = TypeCertificationFormation.Attestation,
                Code = "ATT-TEST-001",
                ScoreFinal = 92,
                Mention = "Excellent"
            });
        });

        using var client = factory.CreateAuthenticatedClient(scoutUser.Id, "Scout");

        var response = await client.GetAsync("/Certificats/MesCertificats");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Mes certificats");
        html.Should().Contain("Orientation scoute");
        html.Should().Contain("ATT-TEST-001");
    }
}
