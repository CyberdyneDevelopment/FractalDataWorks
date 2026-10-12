using Fdw.Messages;

namespace Fdw.Messages.Tests;

/// <summary>
/// Tests for standard message classes.
/// </summary>
public class StandardMessageTests
{
    #region ArgumentNullMessage Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ArgumentNullMessage_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        const string parameterName = "userId";

        // Act
        var message = new ArgumentNullMessage(parameterName);

        // Assert
        message.ParameterName.ShouldBe(parameterName);
        message.Id.ShouldBe(1001);
        message.Name.ShouldBe("ArgumentNull");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe($"Parameter '{parameterName}' cannot be null");
        message.Code.ShouldBe("ARG_NULL");
        message.Source.ShouldBe("ArgumentValidation");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    [InlineData("parameter1")]
    [InlineData("myVariable")]
    [InlineData("config")]
    public void ArgumentNullMessage_Constructor_HandlesVariousParameterNames(string parameterName)
    {
        // Act
        var message = new ArgumentNullMessage(parameterName);

        // Assert
        message.ParameterName.ShouldBe(parameterName);
        message.Message.ShouldContain(parameterName);
    }

    #endregion

    #region ErrorMessage Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ErrorMessage_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        const string errorDescription = "Database connection failed";

        // Act
        var message = new ErrorMessage(errorDescription);

        // Assert
        message.ErrorDescription.ShouldBe(errorDescription);
        message.Id.ShouldBe(1003);
        message.Name.ShouldBe("Error");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe(errorDescription);
        message.Code.ShouldBe("ERROR");
        message.Source.ShouldBe("General");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    [InlineData("File not accessible")]
    [InlineData("Network timeout")]
    [InlineData("Invalid operation")]
    public void ErrorMessage_Constructor_HandlesVariousErrors(string errorDescription)
    {
        // Act
        var message = new ErrorMessage(errorDescription);

        // Assert
        message.ErrorDescription.ShouldBe(errorDescription);
        message.Message.ShouldBe(errorDescription);
    }

    #endregion

    #region NotFoundMessage Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void NotFoundMessage_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        const string resourceDescription = "User with ID 123";

        // Act
        var message = new NotFoundMessage(resourceDescription);

        // Assert
        message.ResourceDescription.ShouldBe(resourceDescription);
        message.Id.ShouldBe(1002);
        message.Name.ShouldBe("NotFound");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe(resourceDescription);
        message.Code.ShouldBe("NOT_FOUND");
        message.Source.ShouldBe("ResourceLookup");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    [InlineData("Order #12345")]
    [InlineData("Configuration key 'ApiUrl'")]
    [InlineData("Document with GUID abc123")]
    public void NotFoundMessage_Constructor_HandlesVariousResources(string resourceDescription)
    {
        // Act
        var message = new NotFoundMessage(resourceDescription);

        // Assert
        message.ResourceDescription.ShouldBe(resourceDescription);
        message.Message.ShouldBe(resourceDescription);
    }

    #endregion

    #region NotImplementedMessage Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void NotImplementedMessage_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        const string featureDescription = "Export to PDF functionality";

        // Act
        var message = new NotImplementedMessage(featureDescription);

        // Assert
        message.FeatureDescription.ShouldBe(featureDescription);
        message.Id.ShouldBe(1005);
        message.Name.ShouldBe("NotImplemented");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe(featureDescription);
        message.Code.ShouldBe("NOT_IMPLEMENTED");
        message.Source.ShouldBe("Implementation");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    [InlineData("OAuth2 authentication")]
    [InlineData("Real-time synchronization")]
    [InlineData("Advanced reporting")]
    public void NotImplementedMessage_Constructor_HandlesVariousFeatures(string featureDescription)
    {
        // Act
        var message = new NotImplementedMessage(featureDescription);

        // Assert
        message.FeatureDescription.ShouldBe(featureDescription);
        message.Message.ShouldBe(featureDescription);
    }

    #endregion

    #region ValidationMessage Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ValidationMessage_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        const string validationError = "Email format is invalid";

        // Act
        var message = new ValidationMessage(validationError);

        // Assert
        message.ValidationError.ShouldBe(validationError);
        message.Id.ShouldBe(1004);
        message.Name.ShouldBe("Validation");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe(validationError);
        message.Code.ShouldBe("VALIDATION");
        message.Source.ShouldBe("Validation");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    [InlineData("Password must be at least 8 characters")]
    [InlineData("Username is required")]
    [InlineData("Age must be between 18 and 120")]
    public void ValidationMessage_Constructor_HandlesVariousValidationErrors(string validationError)
    {
        // Act
        var message = new ValidationMessage(validationError);

        // Assert
        message.ValidationError.ShouldBe(validationError);
        message.Message.ShouldBe(validationError);
    }

    #endregion
}
