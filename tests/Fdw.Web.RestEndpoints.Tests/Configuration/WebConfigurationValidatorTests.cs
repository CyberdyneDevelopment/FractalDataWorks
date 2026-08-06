using FluentValidation.TestHelper;
using Fdw.Web.RestEndpoints.Configuration;

namespace Fdw.Web.RestEndpoints.Tests.Configuration;

public class WebConfigurationValidatorTests
{
    private readonly WebConfigurationValidator _validator;

    public WebConfigurationValidatorTests()
    {
        _validator = new WebConfigurationValidator();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Validate_HostIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var config = new WebConfiguration { Host = string.Empty };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Host);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Validate_HostIsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var config = new WebConfiguration { Host = "localhost" };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Host);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Validate_PortIsZero_ShouldHaveValidationError()
    {
        // Arrange
        var config = new WebConfiguration { Port = 0 };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Port);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Validate_PortIsNegative_ShouldHaveValidationError()
    {
        // Arrange
        var config = new WebConfiguration { Port = -1 };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Port);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Validate_PortIsGreaterThan65535_ShouldHaveValidationError()
    {
        // Arrange
        var config = new WebConfiguration { Port = 65536 };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Port);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Validate_PortIsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var config = new WebConfiguration { Port = 8080 };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Port);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Validate_PortIs1_ShouldNotHaveValidationError()
    {
        // Arrange
        var config = new WebConfiguration { Port = 1 };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Port);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Validate_PortIs65535_ShouldNotHaveValidationError()
    {
        // Arrange
        var config = new WebConfiguration { Port = 65535 };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Port);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Validate_ValidConfiguration_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var config = new WebConfiguration
        {
            Host = "localhost",
            Port = 5000
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Validate_HostIsNull_ShouldHaveValidationError()
    {
        // Arrange
        var config = new WebConfiguration { Host = null! };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Host);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Validate_PortAtMinimumBoundary_ShouldNotHaveValidationError()
    {
        // Arrange
        var config = new WebConfiguration { Host = "localhost", Port = 1 };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Port);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Validate_PortAtMaximumBoundary_ShouldNotHaveValidationError()
    {
        // Arrange
        var config = new WebConfiguration { Host = "localhost", Port = 65535 };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Port);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Validate_ValidHost_ShouldNotHaveValidationError()
    {
        // Arrange
        var config = new WebConfiguration { Host = "example.com", Port = 443 };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Host);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Validate_ValidIPv4Host_ShouldNotHaveValidationError()
    {
        // Arrange
        var config = new WebConfiguration { Host = "192.168.1.1", Port = 8080 };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Host);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Validate_StandardHttpPort_ShouldNotHaveValidationError()
    {
        // Arrange
        var config = new WebConfiguration { Host = "localhost", Port = 80 };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Port);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Validate_StandardHttpsPort_ShouldNotHaveValidationError()
    {
        // Arrange
        var config = new WebConfiguration { Host = "localhost", Port = 443 };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Port);
    }
}
