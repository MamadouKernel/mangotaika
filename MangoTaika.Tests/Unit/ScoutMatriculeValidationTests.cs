using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using MangoTaika.DTOs;
using MangoTaika.Models;
using Xunit;

namespace MangoTaika.Tests.Unit;

public sealed class ScoutMatriculeValidationTests
{
    [Fact]
    public void ScoutCreateDto_Accepts_ExpectedMatriculeFormat()
    {
        var model = new ScoutCreateDto
        {
            Matricule = "0583753X",
            Nom = "Doe",
            Prenom = "Jane",
            DateNaissance = new DateTime(2012, 5, 14)
        };

        Validate(model).Should().BeEmpty();
    }

    [Theory]
    [InlineData("MT-2026-00001")]
    [InlineData("0583753")]
    [InlineData("0583753XX")]
    [InlineData("058375-3X")]
    public void ScoutCreateDto_Rejects_InvalidMatriculeFormats(string matricule)
    {
        var model = new ScoutCreateDto
        {
            Matricule = matricule,
            Nom = "Doe",
            Prenom = "Jane",
            DateNaissance = new DateTime(2012, 5, 14)
        };

        Validate(model)
            .Should()
            .Contain(result => result.MemberNames.Contains(nameof(ScoutCreateDto.Matricule)));
    }

    [Fact]
    public void RegisterViewModel_Accepts_CommaSeparatedMatricules_WhenAllAreValid()
    {
        var model = new RegisterViewModel
        {
            Nom = "Doe",
            Prenom = "Jane",
            Telephone = "0701020304",
            Password = "User@2026!",
            ConfirmPassword = "User@2026!",
            Role = "Parent",
            Consentement = true,
            Matricules = "0583753X, 0583754X"
        };

        Validate(model).Should().BeEmpty();
    }

    [Fact]
    public void RegisterViewModel_Rejects_CommaSeparatedMatricules_WhenOneIsInvalid()
    {
        var model = new RegisterViewModel
        {
            Nom = "Doe",
            Prenom = "Jane",
            Telephone = "0701020304",
            Password = "User@2026!",
            ConfirmPassword = "User@2026!",
            Role = "Parent",
            Consentement = true,
            Matricules = "0583753X, MT-2026-00001"
        };

        Validate(model)
            .Should()
            .Contain(result => result.MemberNames.Contains(nameof(RegisterViewModel.Matricules)));
    }

    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
