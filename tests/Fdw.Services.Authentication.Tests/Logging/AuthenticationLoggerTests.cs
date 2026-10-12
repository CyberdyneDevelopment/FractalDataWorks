using System;
using Fdw.Services.Authentication.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Tests.Logging;

/// <summary>
/// Unit tests for the <see cref="AuthenticationLogger"/> MessageLogging methods — verifies each
/// method's EventId, level, and generated <c>"AUTHENTICATION-{EventId}"</c> message code, and that
/// the underlying <see cref="ILogger.Log"/> call fires exactly once per invocation.
/// </summary>
public sealed class AuthenticationLoggerTests
{
    private readonly Mock<ILogger> _logger = new();

    public AuthenticationLoggerTests()
    {
        _logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
    }

    private void VerifyLogged(LogLevel level, int eventId) =>
        _logger.Verify(
            l => l.Log(
                level,
                new EventId(eventId),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void GettingAuthenticationServiceProducesDebugMessageWithCorrectCode()
    {
        var result = AuthenticationLogger.GettingAuthenticationService(_logger.Object, "Jwt");

        result.Code.ShouldBe("AUTHENTICATION-11003");
        result.Message.ShouldContain("Jwt");
        VerifyLogged(LogLevel.Debug, 11003);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void UnknownAuthenticationTypeProducesWarningMessageWithCorrectCode()
    {
        var result = AuthenticationLogger.UnknownAuthenticationType(_logger.Object, "Bogus");

        result.Code.ShouldBe("AUTHENTICATION-21000");
        VerifyLogged(LogLevel.Warning, 21000);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void NoFactoryRegisteredProducesErrorMessageWithCorrectCode()
    {
        var result = AuthenticationLogger.NoFactoryRegistered(_logger.Object, "Jwt");

        result.Code.ShouldBe("AUTHENTICATION-61002");
        VerifyLogged(LogLevel.Error, 61002);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AuthenticationServiceCreatedProducesDebugMessageWithCorrectCode()
    {
        var result = AuthenticationLogger.AuthenticationServiceCreated(_logger.Object, "Jwt");

        result.Code.ShouldBe("AUTHENTICATION-11004");
        VerifyLogged(LogLevel.Debug, 11004);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AuthenticationServiceCreationFailedProducesErrorMessageWithCorrectCode()
    {
        var result = AuthenticationLogger.AuthenticationServiceCreationFailed(_logger.Object, "Jwt", "boom");

        result.Code.ShouldBe("AUTHENTICATION-91001");
        result.Message.ShouldContain("boom");
        VerifyLogged(LogLevel.Error, 91001);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AuthenticationServiceCreationExceptionProducesErrorMessageWithCorrectCode()
    {
        var result = AuthenticationLogger.AuthenticationServiceCreationException(
            _logger.Object, new InvalidOperationException("nope"), "Jwt");

        result.Code.ShouldBe("AUTHENTICATION-91002");
        VerifyLogged(LogLevel.Error, 91002);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void GettingAuthenticationServiceByConfigurationNameProducesDebugMessageWithCorrectCode()
    {
        var result = AuthenticationLogger.GettingAuthenticationServiceByConfigurationName(_logger.Object, "Primary");

        result.Code.ShouldBe("AUTHENTICATION-11005");
        VerifyLogged(LogLevel.Debug, 11005);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConfigurationSectionNotFoundProducesWarningMessageWithCorrectCode()
    {
        var result = AuthenticationLogger.ConfigurationSectionNotFound(_logger.Object, "Primary");

        result.Code.ShouldBe("AUTHENTICATION-61003");
        VerifyLogged(LogLevel.Warning, 61003);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AuthenticationTypeNotSpecifiedProducesWarningMessageWithCorrectCode()
    {
        var result = AuthenticationLogger.AuthenticationTypeNotSpecified(_logger.Object, "Primary");

        result.Code.ShouldBe("AUTHENTICATION-61004");
        VerifyLogged(LogLevel.Warning, 61004);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void UnknownAuthenticationTypeInConfigurationProducesWarningMessageWithCorrectCode()
    {
        var result = AuthenticationLogger.UnknownAuthenticationTypeInConfiguration(_logger.Object, "Bogus");

        result.Code.ShouldBe("AUTHENTICATION-61005");
        VerifyLogged(LogLevel.Warning, 61005);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConfigurationBindingFailedProducesErrorMessageWithCorrectCode()
    {
        var result = AuthenticationLogger.ConfigurationBindingFailed(_logger.Object, "JwtAuthenticationConfiguration");

        result.Code.ShouldBe("AUTHENTICATION-91003");
        VerifyLogged(LogLevel.Error, 91003);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void ConfigurationBindingFailedAllowsNullConfigurationType()
    {
        var result = AuthenticationLogger.ConfigurationBindingFailed(_logger.Object, null);

        result.Code.ShouldBe("AUTHENTICATION-91003");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void GetAuthenticationServiceByNameExceptionProducesErrorMessageWithCorrectCode()
    {
        var result = AuthenticationLogger.GetAuthenticationServiceByNameException(
            _logger.Object, new InvalidOperationException("nope"), "Primary");

        result.Code.ShouldBe("AUTHENTICATION-91004");
        VerifyLogged(LogLevel.Error, 91004);
    }
}
