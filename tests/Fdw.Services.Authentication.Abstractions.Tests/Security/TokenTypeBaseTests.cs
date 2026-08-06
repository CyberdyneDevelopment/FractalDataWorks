using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;

namespace Fdw.Services.Authentication.Abstractions.Tests.Security;

/// <summary>
/// Tests for TokenTypeBase class.
/// </summary>
public class TokenTypeBaseTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorInitializesAllProperties()
    {
        // Arrange & Act
        var tokenType = new TestTokenType(
            id: 1,
            name: "AccessToken",
            format: "JWT",
            canBeRefreshed: true,
            containsUserIdentity: true,
            typicalLifetimeSeconds: 3600);

        // Assert
        tokenType.Id.ShouldBe(1);
        tokenType.Name.ShouldBe("AccessToken");
        tokenType.Format.ShouldBe("JWT");
        tokenType.CanBeRefreshed.ShouldBeTrue();
        tokenType.ContainsUserIdentity.ShouldBeTrue();
        tokenType.TypicalLifetimeSeconds.ShouldBe(3600);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorInitializesWithNullLifetime()
    {
        // Arrange & Act
        var tokenType = new TestTokenType(
            id: 2,
            name: "RefreshToken",
            format: "Opaque",
            canBeRefreshed: false,
            containsUserIdentity: false,
            typicalLifetimeSeconds: null);

        // Assert
        tokenType.TypicalLifetimeSeconds.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorInitializesNonRefreshableToken()
    {
        // Arrange & Act
        var tokenType = new TestTokenType(
            id: 3,
            name: "IdToken",
            format: "JWT",
            canBeRefreshed: false,
            containsUserIdentity: true,
            typicalLifetimeSeconds: 3600);

        // Assert
        tokenType.CanBeRefreshed.ShouldBeFalse();
        tokenType.ContainsUserIdentity.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void PropertiesAreReadOnly()
    {
        // Arrange
        var tokenType = new TestTokenType(1, "Test", "JWT", true, true, 3600);

        // Assert - Properties should only have getters
        var type = typeof(ITokenType);
        type.GetProperty(nameof(ITokenType.Format))!.CanWrite.ShouldBeFalse();
        type.GetProperty(nameof(ITokenType.CanBeRefreshed))!.CanWrite.ShouldBeFalse();
        type.GetProperty(nameof(ITokenType.ContainsUserIdentity))!.CanWrite.ShouldBeFalse();
        type.GetProperty(nameof(ITokenType.TypicalLifetimeSeconds))!.CanWrite.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TokenTypeImplementsITokenType()
    {
        // Arrange
        var tokenType = new TestTokenType(1, "Test", "JWT", true, true, 3600);

        // Assert
        tokenType.ShouldBeAssignableTo<ITokenType>();
    }

    /// <summary>
    /// Testable implementation of TokenTypeBase.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private sealed class TestTokenType : TokenTypeBase
    {
        public TestTokenType(
            int id,
            string name,
            string format,
            bool canBeRefreshed,
            bool containsUserIdentity,
            int? typicalLifetimeSeconds)
            : base(id, name, format, canBeRefreshed, containsUserIdentity, typicalLifetimeSeconds)
        {
        }
    }
}
