using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Messages;

namespace Fdw.Services.Authentication.Abstractions.Tests.Messages;

/// <summary>
/// Tests for all concrete authentication message types.
/// </summary>
public class AllMessageTypesTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConfigurationNullMessageInitializesCorrectly()
    {
        // Arrange & Act
        var message = new ConfigurationNullMessage();

        // Assert
        message.Id.ShouldBe(1001);
        message.Name.ShouldBe("ConfigurationNull");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Configuration cannot be null");
        message.Code.ShouldBe("AUTH_CONFIG_NULL");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConfigurationNameNullMessageInitializesCorrectly()
    {
        // Arrange & Act
        var message = new ConfigurationNameNullMessage();

        // Assert
        message.Id.ShouldBe(1004);
        message.Name.ShouldBe("ConfigurationNameNull");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Configuration name cannot be null or empty");
        message.Code.ShouldBe("AUTH_CONFIG_NAME_NULL");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConfigurationSectionNotFoundMessageInitializesCorrectly()
    {
        // Arrange & Act
        var message = new ConfigurationSectionNotFoundMessage("TestSection");

        // Assert
        message.Id.ShouldBe(1005);
        message.Name.ShouldBe("ConfigurationSectionNotFound");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Configuration section not found: Authentication:TestSection");
        message.Code.ShouldBe("AUTH_CONFIG_NOT_FOUND");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConfigurationBindingFailedMessageInitializesCorrectly()
    {
        // Arrange & Act
        var message = new ConfigurationBindingFailedMessage("TestConfig");

        // Assert
        message.Id.ShouldBe(1007);
        message.Name.ShouldBe("ConfigurationBindingFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Failed to bind configuration to TestConfig");
        message.Code.ShouldBe("AUTH_CONFIG_BINDING_FAILED");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void InvalidTokenMessageDefaultConstructor()
    {
        // Arrange & Act
        var message = new InvalidTokenMessage();

        // Assert
        message.Id.ShouldBe(2001);
        message.Name.ShouldBe("InvalidToken");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("The provided token is invalid");
        message.Code.ShouldBe("AUTH_INVALID_TOKEN");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void InvalidTokenMessageWithReason()
    {
        // Arrange & Act
        var message = new InvalidTokenMessage("Signature mismatch");

        // Assert
        message.Message.ShouldBe("The provided token is invalid: Signature mismatch");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TokenExpiredMessageDefaultConstructor()
    {
        // Arrange & Act
        var message = new TokenExpiredMessage();

        // Assert
        message.Id.ShouldBe(2002);
        message.Name.ShouldBe("TokenExpired");
        message.Severity.ShouldBe(MessageSeverity.Warning);
        message.Message.ShouldBe("The authentication token has expired");
        message.Code.ShouldBe("AUTH_TOKEN_EXPIRED");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TokenExpiredMessageWithTimestamp()
    {
        // Arrange & Act
        var message = new TokenExpiredMessage("2024-01-15 10:30:00");

        // Assert
        message.Message.ShouldBe("The authentication token expired at 2024-01-15 10:30:00");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TokenNullOrEmptyMessageInitializesCorrectly()
    {
        // Arrange & Act
        var message = new TokenNullOrEmptyMessage();

        // Assert
        message.Id.ShouldBe(2001);
        message.Name.ShouldBe("TokenNullOrEmpty");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Token cannot be null or empty");
        message.Code.ShouldBe("AUTH_TOKEN_NULL");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void RefreshTokenInvalidMessageInitializesCorrectly()
    {
        // Arrange & Act
        var message = new RefreshTokenInvalidMessage();

        // Assert
        message.Id.ShouldBe(2004);
        message.Name.ShouldBe("RefreshTokenInvalid");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("The refresh token is invalid or has been revoked");
        message.Code.ShouldBe("AUTH_REFRESH_TOKEN_INVALID");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TokenRevocationFailedMessageInitializesCorrectly()
    {
        // Arrange & Act
        var message = new TokenRevocationFailedMessage("Network timeout");

        // Assert
        message.Id.ShouldBe(2002);
        message.Name.ShouldBe("TokenRevocationFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Failed to revoke token: Network timeout");
        message.Code.ShouldBe("AUTH_REVOKE_FAILED");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AuthenticationTypeNotSpecifiedMessageInitializesCorrectly()
    {
        // Arrange & Act
        var message = new AuthenticationTypeNotSpecifiedMessage("TestSection");

        // Assert
        message.Id.ShouldBe(1006);
        message.Name.ShouldBe("AuthenticationTypeNotSpecified");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("AuthenticationType not specified in configuration section: TestSection");
        message.Code.ShouldBe("AUTH_TYPE_NOT_SPECIFIED");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void UnknownAuthenticationTypeMessageInitializesCorrectly()
    {
        // Arrange & Act
        var message = new UnknownAuthenticationTypeMessage("CustomAuth");

        // Assert
        message.Id.ShouldBe(1002);
        message.Name.ShouldBe("UnknownAuthenticationType");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Unknown authentication type: CustomAuth");
        message.Code.ShouldBe("AUTH_UNKNOWN_TYPE");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void NoFactoryRegisteredMessageInitializesCorrectly()
    {
        // Arrange & Act
        var message = new NoFactoryRegisteredMessage("OAuth2");

        // Assert
        message.Id.ShouldBe(1003);
        message.Name.ShouldBe("NoFactoryRegistered");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("No factory registered for authentication type: OAuth2");
        message.Code.ShouldBe("AUTH_NO_FACTORY");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ServiceCreationExceptionMessageInitializesCorrectly()
    {
        // Arrange & Act
        var message = new ServiceCreationExceptionMessage("Missing configuration");

        // Assert
        message.Id.ShouldBe(1008);
        message.Name.ShouldBe("ServiceCreationException");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Missing configuration");
        message.Code.ShouldBe("AUTH_SERVICE_CREATION_FAILED");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CommandExecutionNotSupportedMessageInitializesCorrectly()
    {
        // Arrange & Act
        var message = new CommandExecutionNotSupportedMessage();

        // Assert
        message.Id.ShouldBe(2004);
        message.Name.ShouldBe("CommandExecutionNotSupported");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Authentication service does not support command-based execution. Use direct methods instead.");
        message.Code.ShouldBe("AUTH_CMD_NOT_SUPPORTED");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AllMessagesAreSealedClasses()
    {
        // Arrange
        var messageTypes = new[]
        {
            typeof(ConfigurationNullMessage),
            typeof(ConfigurationNameNullMessage),
            typeof(ConfigurationSectionNotFoundMessage),
            typeof(ConfigurationBindingFailedMessage),
            typeof(InvalidTokenMessage),
            typeof(TokenExpiredMessage),
            typeof(AuthenticationFailedMessage),
            typeof(TokenNullOrEmptyMessage),
            typeof(RefreshTokenInvalidMessage),
            typeof(TokenRevocationFailedMessage),
            typeof(AuthenticationTypeNotSpecifiedMessage),
            typeof(UnknownAuthenticationTypeMessage),
            typeof(NoFactoryRegisteredMessage),
            typeof(ServiceCreationExceptionMessage),
            typeof(CommandExecutionNotSupportedMessage)
        };

        // Assert
        foreach (var type in messageTypes)
        {
            type.IsSealed.ShouldBeTrue($"{type.Name} should be sealed");
            type.IsClass.ShouldBeTrue($"{type.Name} should be a class");
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AllMessagesInheritFromAuthenticationMessage()
    {
        // Arrange
        var messages = new IServiceMessage[]
        {
            new ConfigurationNullMessage(),
            new ConfigurationNameNullMessage(),
            new ConfigurationSectionNotFoundMessage("Test"),
            new ConfigurationBindingFailedMessage("Test"),
            new InvalidTokenMessage(),
            new TokenExpiredMessage(),
            new AuthenticationFailedMessage(),
            new TokenNullOrEmptyMessage(),
            new RefreshTokenInvalidMessage(),
            new TokenRevocationFailedMessage("Test"),
            new AuthenticationTypeNotSpecifiedMessage("Test"),
            new UnknownAuthenticationTypeMessage("Test"),
            new NoFactoryRegisteredMessage("Test"),
            new ServiceCreationExceptionMessage("Test"),
            new CommandExecutionNotSupportedMessage()
        };

        // Assert
        foreach (var message in messages)
        {
            message.ShouldBeAssignableTo<AuthenticationMessage>($"{message.GetType().Name} should inherit from AuthenticationMessage");
        }
    }
}
