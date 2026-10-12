using System.Reflection;

namespace Fdw.Validation.Tests;

public sealed class ValidationServiceExtensionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AddFrameworkValidationRegistersValidatorsFromAssembly()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddFrameworkValidation(assembly);
        var provider = services.BuildServiceProvider();

        // Assert
        var validator = provider.GetService<IValidator<TestRequest>>();
        validator.ShouldNotBeNull();
        validator.ShouldBeAssignableTo<FdwValidator<TestRequest>>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AddFrameworkValidationReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        var result = services.AddFrameworkValidation(assembly);

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidateWithFdwRegistersIValidateOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions<TestConfiguration>();

        // Act
        services.AddOptions<TestConfiguration>()
            .ValidateWithFdw<TestConfiguration, TestConfigurationValidator>();

        var provider = services.BuildServiceProvider();

        // Assert
        var validateOptions = provider.GetService<IValidateOptions<TestConfiguration>>();
        validateOptions.ShouldNotBeNull();
        validateOptions.ShouldBeOfType<TestConfigurationValidator>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidateWithFdwChainsOptionsBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions<TestConfiguration>();

        // Act
        var builder = services.AddOptions<TestConfiguration>()
            .ValidateWithFdw<TestConfiguration, TestConfigurationValidator>();

        // Assert
        builder.ShouldNotBeNull();
        builder.ShouldBeOfType<OptionsBuilder<TestConfiguration>>();
    }
}
