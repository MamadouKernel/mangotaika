using FluentAssertions;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using MangoTaika.Services;
using MangoTaika.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MangoTaika.Tests.Integration;

public sealed class GroupeServiceIntegrationTests
{
    [Fact]
    public async Task CreateAsync_Builds_Address_And_Uses_Geocoding_Result()
    {
        await using var db = TestDbContextFactory.CreateDbContext();
        var service = new GroupeService(db, new FakeGeocodingService());

        var created = await service.CreateAsync(new GroupeCreateDto
        {
            Nom = " Groupe Riviera ",
            Description = " Groupe test ",
            Commune = "Cocody",
            Quartier = "Riviera 2",
            NomChefGroupe = " Chef Principal ",
            Latitude = 1.5,
            Longitude = 2.5
        });

        var groupe = await db.Groupes.SingleAsync();

        created.Nom.Should().Be("Groupe Riviera");
        created.Adresse.Should().Be("Riviera 2, Cocody");
        created.Latitude.Should().Be(5.3364);
        created.Longitude.Should().Be(-4.0267);
        groupe.Description.Should().Be("Groupe test");
        groupe.NomChefGroupe.Should().Be("Chef Principal");
    }

    [Fact]
    public async Task CreateAsync_Rejects_Inactive_Responsable()
    {
        await using var db = TestDbContextFactory.CreateDbContext();
        var responsable = await TestDataSeeder.AddUserAsync(db, "Awa", "Responsable", [], isActive: false);
        var service = new GroupeService(db, new FakeGeocodingService());

        Func<Task> act = () => service.CreateAsync(new GroupeCreateDto
        {
            Nom = "Groupe Riviera",
            ResponsableId = responsable.Id
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*responsable selectionne est introuvable ou inactif*");

        db.Groupes.Should().BeEmpty();
    }
}
