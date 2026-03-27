using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using MangoTaika.DTOs;
using Xunit;

namespace MangoTaika.Tests.Unit;

public sealed class GroupeValidationTests
{
    [Fact]
    public void GroupeCreateDto_Rejects_Missing_Name()
    {
        var model = new GroupeCreateDto();

        Validate(model)
            .Should()
            .Contain(result => result.MemberNames.Contains(nameof(GroupeCreateDto.Nom)));
    }

    [Fact]
    public void GroupeCreateDto_Rejects_Invalid_Latitude()
    {
        var model = new GroupeCreateDto
        {
            Nom = "Groupe Riviera",
            Latitude = 91
        };

        Validate(model)
            .Should()
            .Contain(result => result.MemberNames.Contains(nameof(GroupeCreateDto.Latitude)));
    }

    [Fact]
    public void GroupeCreateDto_Rejects_Invalid_Longitude()
    {
        var model = new GroupeCreateDto
        {
            Nom = "Groupe Riviera",
            Longitude = -181
        };

        Validate(model)
            .Should()
            .Contain(result => result.MemberNames.Contains(nameof(GroupeCreateDto.Longitude)));
    }

    [Fact]
    public void GroupeCreateDto_Accepts_Valid_Data()
    {
        var model = new GroupeCreateDto
        {
            Nom = "Groupe Riviera",
            Latitude = 5.3364,
            Longitude = -4.0267
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
