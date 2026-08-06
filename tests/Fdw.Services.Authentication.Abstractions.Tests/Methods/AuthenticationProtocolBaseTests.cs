using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Methods;

namespace Fdw.Services.Authentication.Abstractions.Tests.Methods;

/// <summary>
/// Tests for AuthenticationProtocolBase class.
/// </summary>
public class AuthenticationProtocolBaseTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorInitializesAllProperties()
    {
        // Arrange & Act
        var protocol = new TestAuthenticationProtocol(
            id: 1,
            name: "OAuth2",
            version: "2.0",
            requiresSecureTransport: true,
            supportsTokens: true);

        // Assert
        protocol.Id.ShouldBe(1);
        protocol.Name.ShouldBe("OAuth2");
        protocol.Version.ShouldBe("2.0");
        protocol.RequiresSecureTransport.ShouldBeTrue();
        protocol.SupportsTokens.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorInitializesInsecureProtocol()
    {
        // Arrange & Act
        var protocol = new TestAuthenticationProtocol(
            id: 2,
            name: "Basic",
            version: "1.0",
            requiresSecureTransport: false,
            supportsTokens: false);

        // Assert
        protocol.RequiresSecureTransport.ShouldBeFalse();
        protocol.SupportsTokens.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void PropertiesAreReadOnly()
    {
        // Arrange
        var protocol = new TestAuthenticationProtocol(1, "Test", "1.0", true, true);

        // Assert - Properties should only have getters
        var type = typeof(IAuthenticationProtocol);
        type.GetProperty(nameof(IAuthenticationProtocol.Version))!.CanWrite.ShouldBeFalse();
        type.GetProperty(nameof(IAuthenticationProtocol.RequiresSecureTransport))!.CanWrite.ShouldBeFalse();
        type.GetProperty(nameof(IAuthenticationProtocol.SupportsTokens))!.CanWrite.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ProtocolImplementsIAuthenticationProtocol()
    {
        // Arrange
        var protocol = new TestAuthenticationProtocol(1, "Test", "1.0", true, true);

        // Assert
        protocol.ShouldBeAssignableTo<IAuthenticationProtocol>();
    }

    /// <summary>
    /// Testable implementation of AuthenticationProtocolBase.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private sealed class TestAuthenticationProtocol : AuthenticationProtocolBase
    {
        public TestAuthenticationProtocol(
            int id,
            string name,
            string version,
            bool requiresSecureTransport,
            bool supportsTokens)
            : base(id, name, version, requiresSecureTransport, supportsTokens)
        {
        }
    }
}
