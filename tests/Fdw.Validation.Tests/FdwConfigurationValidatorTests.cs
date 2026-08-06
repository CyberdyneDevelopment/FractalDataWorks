namespace Fdw.Validation.Tests;

public sealed class TestConfiguration
{
    public string Name { get; set; } = string.Empty;
    public int Port { get; set; }
}

public sealed class TestConfigurationValidator : FdwConfigurationValidator<TestConfiguration>
{
    public TestConfigurationValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Port).GreaterThan(0);
    }
}

public sealed class FdwConfigurationValidatorTests
{
    private readonly TestConfigurationValidator _validator = new();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidateReturnsSuccessForValidConfiguration()
    {
        // Arrange
        var config = new TestConfiguration { Name = "TestConfig", Port = 8080 };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.ShouldBe(ValidateOptionsResult.Success);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidateReturnsFailureForInvalidConfiguration()
    {
        // Arrange
        var config = new TestConfiguration { Name = "", Port = 0 };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Failed.ShouldBeTrue();
        result.Failures.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidateReturnsAllValidationErrors()
    {
        // Arrange
        var config = new TestConfiguration { Name = "", Port = 0 };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Failed.ShouldBeTrue();
        result.Failures.Count().ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidateIgnoresNameParameter()
    {
        // Arrange
        var config = new TestConfiguration { Name = "TestConfig", Port = 8080 };

        // Act
        var resultWithName = _validator.Validate("test", config);
        var resultWithNull = _validator.Validate(null, config);

        // Assert
        resultWithName.ShouldBe(ValidateOptionsResult.Success);
        resultWithNull.ShouldBe(ValidateOptionsResult.Success);
    }
}
