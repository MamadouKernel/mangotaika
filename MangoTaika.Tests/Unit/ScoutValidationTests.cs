using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using MangoTaika.DTOs;
using Xunit;

namespace MangoTaika.Tests.Unit;

public sealed class ScoutValidationTests
{
    [Fact]
    public void ScoutCreateDto_Rejects_Future_DateNaissance()
    {
        var model = new ScoutCreateDto
        {
            Matricule = "0583753X",
            Nom = "Kone",
            Prenom = "Awa",
            DateNaissance = DateTime.UtcNow.Date.AddDays(1)
        };

        Validate(model)
            .Should()
            .Contain(result => result.MemberNames.Contains(nameof(ScoutCreateDto.DateNaissance)));
    }

    [Fact]
    public void ScoutCreateDto_Rejects_Branche_Without_Groupe()
    {
        var model = new ScoutCreateDto
        {
            Matricule = "0583753X",
            Nom = "Kone",
            Prenom = "Awa",
            DateNaissance = new DateTime(2012, 5, 14),
            BrancheId = Guid.NewGuid()
        };

        Validate(model)
            .Should()
            .Contain(result => result.MemberNames.Contains(nameof(ScoutCreateDto.GroupeId)))
            .And
            .Contain(result => result.MemberNames.Contains(nameof(ScoutCreateDto.BrancheId)));
    }

    [Fact]
    public void ScoutCreateDto_Accepts_Valid_Data()
    {
        var model = new ScoutCreateDto
        {
            Matricule = "0583753X",
            Nom = "Kone",
            Prenom = "Awa",
            DateNaissance = new DateTime(2012, 5, 14),
            GroupeId = Guid.NewGuid(),
            BrancheId = Guid.NewGuid()
        };

        Validate(model).Should().BeEmpty();
    }

    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
