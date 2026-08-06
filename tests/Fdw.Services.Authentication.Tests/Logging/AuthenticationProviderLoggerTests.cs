using System;
using Fdw.Services.Authentication.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Tests.Logging;

/// <summary>
/// Unit tests for the <see cref="AuthenticationProviderLogger"/> MessageLogging methods, including
/// the one raw <see cref="LoggerMessageAttribute"/>-generated <c>void</c> method
/// (<see cref="AuthenticationProviderLogger.CacheCleared"/>), which does not return an
/// <c>IGenericMessage</c> and must be asserted purely via the mocked <see cref="ILogger"/> call.
/// </summary>
public sealed class AuthenticationProviderLoggerTests
{
    private readonly Mock<ILogger> _logger = new();

    public AuthenticationProviderLoggerTests()
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
    public void GettingAuthenticationProducesDebugMessageWithCorrectCode()
    {
        var result = AuthenticationProviderLogger.GettingAuthentication(_logger.Object, "Jwt");

        result.Code.ShouldBe("AUTHENTICATION-11006");
        VerifyLogged(LogLevel.Debug, 11006);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void NoFactoryRegisteredProducesErrorMessageWithCorrectCode()
    {
        var result = AuthenticationProviderLogger.NoFactoryRegistered(_logger.Object, "Jwt");

        result.Code.ShouldBe("AUTHENTICATION-61006");
        VerifyLogged(LogLevel.Error, 61006);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void GettingAuthenticationByConfigurationNameProducesDebugMessageWithCorrectCode()
    {
        var result = AuthenticationProviderLogger.GettingAuthenticationByConfigurationName(_logger.Object, "Primary");

        result.Code.ShouldBe("AUTHENTICATION-11007");
        VerifyLogged(LogLevel.Debug, 11007);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AuthenticationConfigurationNotFoundProducesWarningMessageWithCorrectCode()
    {
        var result = AuthenticationProviderLogger.AuthenticationConfigurationNotFound(_logger.Object, "Primary");

        result.Code.ShouldBe("AUTHENTICATION-31000");
        VerifyLogged(LogLevel.Warning, 31000);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConfigurationLoadFailedProducesErrorMessageWithCorrectCode()
    {
        var result = AuthenticationProviderLogger.ConfigurationLoadFailed(_logger.Object, "Primary", "JwtAuthenticationConfiguration");

        result.Code.ShouldBe("AUTHENTICATION-91005");
        VerifyLogged(LogLevel.Error, 91005);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AuthenticationConfigurationLoadedProducesInformationMessageWithCorrectCode()
    {
        var result = AuthenticationProviderLogger.AuthenticationConfigurationLoaded(_logger.Object, "Primary", "Jwt");

        result.Code.ShouldBe("AUTHENTICATION-11008");
        VerifyLogged(LogLevel.Information, 11008);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void FactoryNotRegisteredInDiProducesErrorMessageWithCorrectCode()
    {
        var result = AuthenticationProviderLogger.FactoryNotRegisteredInDi(_logger.Object, "Primary", "JwtAuthenticationServiceFactory");

        result.Code.ShouldBe("AUTHENTICATION-61007");
        VerifyLogged(LogLevel.Error, 61007);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void CreatingAuthenticationWithFactoryProducesDebugMessageWithCorrectCode()
    {
        var result = AuthenticationProviderLogger.CreatingAuthenticationWithFactory(_logger.Object, "Primary", "JwtAuthenticationServiceFactory");

        result.Code.ShouldBe("AUTHENTICATION-11009");
        VerifyLogged(LogLevel.Debug, 11009);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void GetAuthenticationByNameExceptionProducesErrorMessageWithCorrectCode()
    {
        var result = AuthenticationProviderLogger.GetAuthenticationByNameException(
            _logger.Object, new InvalidOperationException("nope"), "Primary");

        result.Code.ShouldBe("AUTHENTICATION-91006");
        VerifyLogged(LogLevel.Error, 91006);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AttemptingTypedAuthenticationProducesDebugMessageWithCorrectCode()
    {
        var result = AuthenticationProviderLogger.AttemptingTypedAuthentication(_logger.Object, "IJwtAuthenticationService");

        result.Code.ShouldBe("AUTHENTICATION-11010");
        VerifyLogged(LogLevel.Debug, 11010);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AuthenticationCastSucceededProducesDebugMessageWithCorrectCode()
    {
        var result = AuthenticationProviderLogger.AuthenticationCastSucceeded(_logger.Object, "IJwtAuthenticationService");

        result.Code.ShouldBe("AUTHENTICATION-11011");
        VerifyLogged(LogLevel.Debug, 11011);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AuthenticationCastFailedProducesWarningMessageWithCorrectCode()
    {
        var result = AuthenticationProviderLogger.AuthenticationCastFailed(_logger.Object, "IJwtAuthenticationService", "OpenIddictAuthenticationService");

        result.Code.ShouldBe("AUTHENTICATION-91007");
        VerifyLogged(LogLevel.Warning, 91007);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ParentProviderRegistrationFailedProducesErrorMessageWithCorrectCode()
    {
        var result = AuthenticationProviderLogger.ParentProviderRegistrationFailed(_logger.Object, "gateway unreachable");

        result.Code.ShouldBe("AUTHENTICATION-61008");
        VerifyLogged(LogLevel.Error, 61008);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void CacheClearedLogsDebugEventWithGivenCount()
    {
        // Act — Why: CacheCleared is a raw [LoggerMessage] (void), not [MessageLogging]; there is
        // no IGenericMessage return value to assert, only the underlying ILogger.Log invocation.
        AuthenticationProviderLogger.CacheCleared(_logger.Object, 7);

        // Assert
        VerifyLogged(LogLevel.Debug, 7214);
    }
}
