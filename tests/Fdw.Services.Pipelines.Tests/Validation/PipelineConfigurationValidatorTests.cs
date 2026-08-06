using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Validators;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Tests.Validation;

/// <summary>
/// Covers every rule branch of <see cref="PipelineConfigurationValidator"/>: the <c>Name</c>
/// pattern/length rule (always applied) and the conditional <c>Description</c> safe-string rule
/// (applied only <c>When(x =&gt; x.Description is not null, ...)</c>), plus the
/// <see cref="Microsoft.Extensions.Options.IValidateOptions{TOptions}"/> success/failure mapping
/// inherited from <c>FdwConfigurationValidator&lt;T&gt;</c>.
/// </summary>
[Trait("Category", "Configuration")]
public sealed class PipelineConfigurationValidatorTests
{
    private readonly PipelineConfigurationValidator _validator = new();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ValidatePassesWithValidNameAndNullDescription()
    {
        // Arrange
        var config = new PipelineConfiguration { Name = "NflIngest", Description = null };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ValidateFailsWithEmptyName()
    {
        var config = new PipelineConfiguration { Name = string.Empty };

        var result = _validator.TestValidate(config);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ValidateFailsWithNameStartingWithADigit()
    {
        var config = new PipelineConfiguration { Name = "1NflIngest" };

        var result = _validator.TestValidate(config);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ValidateFailsWithNameExceedingMaxLength()
    {
        var config = new PipelineConfiguration { Name = new string('A', 201) };

        var result = _validator.TestValidate(config);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ValidatePassesWithValidDescription()
    {
        var config = new PipelineConfiguration { Name = "NflIngest", Description = "A valid pipeline description" };

        var result = _validator.TestValidate(config);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ValidateFailsWithControlCharactersInDescription()
    {
        var config = new PipelineConfiguration { Name = "NflIngest", Description = "Bad\0Description" };

        var result = _validator.TestValidate(config);

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ValidateFailsWithDescriptionExceedingMaxLength()
    {
        var config = new PipelineConfiguration { Name = "NflIngest", Description = new string('A', 1001) };

        var result = _validator.TestValidate(config);

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ValidateOptionsReturnsSuccessForValidConfig()
    {
        var config = new PipelineConfiguration { Name = "NflIngest" };

        var result = _validator.Validate(Options.DefaultName, config);

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ValidateOptionsReturnsFailureForInvalidConfig()
    {
        var config = new PipelineConfiguration { Name = string.Empty };

        var result = _validator.Validate(Options.DefaultName, config);

        result.Failed.ShouldBeTrue();
    }
}
