using Fdw.Services.Quality.Configuration;
using Fdw.Services.Quality.Validators;
using Shouldly;
using Xunit;

namespace Fdw.Services.Quality.Tests;

/// <summary>
/// A field description is still refused without its text (FDW-6208): the column that held it was NOT NULL, but the refusal on a save was always the validator's, run on each child before
/// it is written, and the member stays on the record when it moves to the descriptions table, so the same rule in the same place still sees it.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class DataSetAnnotationFieldDescriptionValidatorTests
{
    private static DataSetAnnotationFieldDescriptionConfiguration Field(string description)
        => new() { Name = "Total", FieldName = "Total", Description = description };

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("")]
    [InlineData("   ")]
    public void AFieldDescriptionWithNoTextIsRefusedWithTheSameMessageAsBefore(string description)
    {
        var validated = new DataSetAnnotationFieldDescriptionConfigurationValidator().Validate(Field(description));

        validated.IsValid.ShouldBeFalse();
        validated.Errors.ShouldContain(error => error.PropertyName == "Description" && error.ErrorMessage == "Description is required");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AFieldDescriptionWithATextIsAccepted()
    {
        new DataSetAnnotationFieldDescriptionConfigurationValidator().Validate(Field("The order total")).IsValid.ShouldBeTrue();
    }
}
