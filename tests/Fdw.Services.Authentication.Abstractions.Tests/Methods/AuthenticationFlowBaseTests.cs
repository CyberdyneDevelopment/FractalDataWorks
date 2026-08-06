using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Methods;

namespace Fdw.Services.Authentication.Abstractions.Tests.Methods;

/// <summary>
/// Tests for AuthenticationFlowBase class.
/// </summary>
public class AuthenticationFlowBaseTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorInitializesAllProperties()
    {
        // Arrange & Act
        var flow = new TestAuthenticationFlow(
            id: 1,
            name: "AuthorizationCode",
            requiresUserInteraction: true,
            supportsRefreshTokens: true,
            isServerToServer: false);

        // Assert
        flow.Id.ShouldBe(1);
        flow.Name.ShouldBe("AuthorizationCode");
        flow.RequiresUserInteraction.ShouldBeTrue();
        flow.SupportsRefreshTokens.ShouldBeTrue();
        flow.IsServerToServer.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorInitializesServerToServerFlow()
    {
        // Arrange & Act
        var flow = new TestAuthenticationFlow(
            id: 2,
            name: "ClientCredentials",
            requiresUserInteraction: false,
            supportsRefreshTokens: false,
            isServerToServer: true);

        // Assert
        flow.RequiresUserInteraction.ShouldBeFalse();
        flow.SupportsRefreshTokens.ShouldBeFalse();
        flow.IsServerToServer.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void PropertiesAreReadOnly()
    {
        // Arrange
        var flow = new TestAuthenticationFlow(1, "Test", true, true, false);

        // Assert - Properties should only have getters
        var type = typeof(IAuthenticationFlow);
        type.GetProperty(nameof(IAuthenticationFlow.RequiresUserInteraction))!.CanWrite.ShouldBeFalse();
        type.GetProperty(nameof(IAuthenticationFlow.SupportsRefreshTokens))!.CanWrite.ShouldBeFalse();
        type.GetProperty(nameof(IAuthenticationFlow.IsServerToServer))!.CanWrite.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void FlowImplementsIAuthenticationFlow()
    {
        // Arrange
        var flow = new TestAuthenticationFlow(1, "Test", true, true, false);

        // Assert
        flow.ShouldBeAssignableTo<IAuthenticationFlow>();
    }

    /// <summary>
    /// Testable implementation of AuthenticationFlowBase.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private sealed class TestAuthenticationFlow : AuthenticationFlowBase
    {
        public TestAuthenticationFlow(
            int id,
            string name,
            bool requiresUserInteraction,
            bool supportsRefreshTokens,
            bool isServerToServer)
            : base(id, name, requiresUserInteraction, supportsRefreshTokens, isServerToServer)
        {
        }
    }
}
