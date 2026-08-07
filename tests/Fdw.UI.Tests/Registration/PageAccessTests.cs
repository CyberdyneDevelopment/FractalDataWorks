using System;
using Fdw.UI.Registration;

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
        // Why this is the whole feature: before this type existed, the registry's most permissive answer
        // was still "any AUTHENTICATED user", so a public page was inexpressible.
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
        // Why HoldsEverything: proves the answer comes from the authentication axis alone. An
        // implementation that consulted permissions would wrongly pass here.
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
        // Why this case matters: an anonymous visitor holds no token and therefore no permission claim, so
        // a predicate answering true for them is answering a question that has no meaning. The
        // authentication axis is checked first precisely so that cannot let anyone through.
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
        // Why it throws rather than degrading to Authenticated: silently widening a page that was meant to
        // be permission-gated is exactly the failure this family exists to prevent.
        Should.Throw<ArgumentException>(() => PageAccess.RequiringPermission(permission!));
    }

    [Fact]
    [Trait("Category", "Registration")]
    public void RequiringPermissionRejectsAMissingPredicate()
    {
        // Why: a caller with no predicate cannot answer the question. Failing is the only honest response —
        // defaulting either way decides a security question by omission.
        Should.Throw<ArgumentNullException>(
            () => PageAccess.RequiringPermission("connections:read").IsSatisfiedBy(isAuthenticated: true, null!));
    }

    [Fact]
    [Trait("Category", "Registration")]
    public void TheDataFreeFormsAreSingletons()
    {
        // Why: they carry no per-page data, so every page declaring one wants the same instance — the same
        // shape as NavItem.Empty, and referenceable the same way.
        PageAccess.Anonymous.ShouldBeSameAs(PageAccess.Anonymous);
        PageAccess.Authenticated.ShouldBeSameAs(PageAccess.Authenticated);
        PageAccess.Anonymous.ShouldNotBeSameAs(PageAccess.Authenticated);
    }
}
