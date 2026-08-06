using System;
using Fdw.Services.Authentication.Abstractions.Security;

namespace Fdw.Services.Authentication.Abstractions.Tests.Security;

/// <summary>
/// Tests for <see cref="SystemAuthenticationContextScope"/> — the boot-safety mechanism that lets
/// host-bootstrap code run under an explicit <see cref="SystemAuthenticationContext"/> elevation
/// without leaking it into request/execution scope.
/// </summary>
public class SystemAuthenticationContextScopeTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorSetsCurrentToSystemAuthenticationContext()
    {
        // Arrange
        var accessor = new AuthenticationContextAccessor();

        // Act
        using var scope = new SystemAuthenticationContextScope(accessor);

        // Assert
        accessor.Current.ShouldNotBeNull();
        accessor.Current.ShouldBeOfType<SystemAuthenticationContext>();
        accessor.Current!.IsSystemContext.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void DisposeRestoresNullWhenAccessorWasEmptyBeforeEntry()
    {
        // Arrange
        var accessor = new AuthenticationContextAccessor();

        // Act
        var scope = new SystemAuthenticationContextScope(accessor);
        scope.Dispose();

        // Assert: the system elevation never survives past the end of the bracketed block.
        accessor.Current.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void DisposeRestoresThePriorValueRatherThanAlwaysNull()
    {
        // Arrange
        // Why: defense in depth — if this scope is ever entered while some other context is already
        // ambient, exiting must restore THAT value, not blindly null it out.
        var accessor = new AuthenticationContextAccessor();
        var previous = new WorkAuthenticationContext(Guid.NewGuid());
        accessor.Current = previous;

        // Act
        var scope = new SystemAuthenticationContextScope(accessor);
        scope.Dispose();

        // Assert
        accessor.Current.ShouldBeSameAs(previous);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void DisposeIsIdempotent()
    {
        // Arrange
        var accessor = new AuthenticationContextAccessor();
        var scope = new SystemAuthenticationContextScope(accessor);

        // Act
        scope.Dispose();
        scope.Dispose();

        // Assert: no exception, and the restored value is not further mutated by the second call.
        accessor.Current.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorThrowsForNullAccessor()
    {
        Should.Throw<ArgumentNullException>(() => new SystemAuthenticationContextScope(null!));
    }
}
