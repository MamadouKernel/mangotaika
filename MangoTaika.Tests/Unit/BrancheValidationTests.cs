using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using MangoTaika.DTOs;
using Xunit;

namespace MangoTaika.Tests.Unit;

public sealed class BrancheValidationTests
{
    [Fact]
    public void BrancheCreateDto_Rejects_AgeMin_GreaterThan_AgeMax()
    {
        var model = new BrancheCreateDto
        {
            Nom = "Louveteaux",
            AgeMin = 13,
            AgeMax = 8,
            GroupeId = Guid.NewGuid(),
            ChefUniteId = Guid.NewGuid()
        };

        Validate(model)
            .Should()
            .Contain(result => result.MemberNames.Contains(nameof(BrancheCreateDto.AgeMin)))
            .And
            .Contain(result => result.MemberNames.Contains(nameof(BrancheCreateDto.AgeMax)));
    }

    [Fact]
    public void BrancheCreateDto_Rejects_Missing_ChefUnite()
    {
        var model = new BrancheCreateDto
        {
            Nom = "Louveteaux",
            AgeMin = 8,
            AgeMax = 12,
            GroupeId = Guid.NewGuid()
        };

        Validate(model)
            .Should()
            .Contain(result => result.MemberNames.Contains(nameof(BrancheCreateDto.ChefUniteId)));
    }

    [Fact]
    public void BrancheCreateDto_Accepts_Valid_Ages_And_ChefUnite()
    {
        var model = new BrancheCreateDto
        {
            Nom = "Louveteaux",
            AgeMin = 8,
            AgeMax = 12,
            GroupeId = Guid.NewGuid(),
            ChefUniteId = Guid.NewGuid()
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
