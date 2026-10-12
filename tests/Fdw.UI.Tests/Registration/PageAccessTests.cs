using System;
using Fdw.UI.Navigation;

namespace Fdw.UI.Tests.Registration;

/// <summary>
/// Pins the rule each <see cref="PageAccess"/> form answers with.
/// </summary>
/// <remarks>
/// Why the two arguments are exercised independently: the point of the type family is that "must the caller
/// be authenticated at all" and "what must they hold" are separate axes. A test that only ever varies them
/// together cannot tell a correct implementation from one that collapses the two.
/// </remarks>
public sealed class PageAccessTests
{
    private static readonly Func<string, bool> HoldsEverything = _ => true;
    private static readonly Func<string, bool> HoldsNothing = _ => false;

    [Fact]
    [Trait("Category", "Registration")]
    public void AnonymousIsSatisfiedByACallerWhoIsNotAuthenticated()
    {
        PageAccess.Anonymous.IsSatisfiedBy(isAuthenticated: false, HoldsNothing).ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "Registration")]
    public void AnonymousIsAlsoSatisfiedByAnAuthenticatedCaller()
    {
        PageAccess.Anonymous.IsSatisfiedBy(isAuthenticated: true, HoldsNothing).ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "Registration")]
    public void AuthenticatedIsNotSatisfiedByAnAnonymousCaller()
    {
        PageAccess.Authenticated.IsSatisfiedBy(isAuthenticated: false, HoldsEverything).ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "Registration")]
    public void AuthenticatedIsSatisfiedByAnyAuthenticatedCallerWhateverTheyHold()
    {
        PageAccess.Authenticated.IsSatisfiedBy(isAuthenticated: true, HoldsNothing).ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "Registration")]
    public void RequiringPermissionIsSatisfiedWhenTheAuthenticatedCallerHoldsIt()
    {
        PageAccess.RequiringPermission("connections:read")
            .IsSatisfiedBy(isAuthenticated: true, p => string.Equals(p, "connections:read", StringComparison.Ordinal))
            .ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "Registration")]
    public void RequiringPermissionIsNotSatisfiedWhenTheCallerHoldsSomethingElse()
    {
        PageAccess.RequiringPermission("connections:read")
            .IsSatisfiedBy(isAuthenticated: true, p => string.Equals(p, "datasets:read", StringComparison.Ordinal))
            .ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "Registration")]
    public void RequiringPermissionIsNotSatisfiedByAnAnonymousCallerEvenIfThePredicateSaysYes()
    {
        PageAccess.RequiringPermission("connections:read")
            .IsSatisfiedBy(isAuthenticated: false, HoldsEverything)
            .ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [Trait("Category", "Registration")]
    public void RequiringPermissionRejectsAMissingPermissionName(string? permission)
    {
        Should.Throw<ArgumentException>(() => PageAccess.RequiringPermission(permission!));
    }

    [Fact]
    [Trait("Category", "Registration")]
    public void RequiringPermissionRejectsAMissingPredicate()
    {
        Should.Throw<ArgumentNullException>(
            () => PageAccess.RequiringPermission("connections:read").IsSatisfiedBy(isAuthenticated: true, null!));
    }

    [Fact]
    [Trait("Category", "Registration")]
    public void TheDataFreeFormsAreSingletons()
    {
        PageAccess.Anonymous.ShouldBeSameAs(PageAccess.Anonymous);
        PageAccess.Authenticated.ShouldBeSameAs(PageAccess.Authenticated);
        PageAccess.Anonymous.ShouldNotBeSameAs(PageAccess.Authenticated);
    }
}
