using System;
using System.Linq;
using Fdw.UI.Navigation;

namespace Fdw.UI.Tests.Registration;

/// <summary>
/// Pins what <see cref="NavTree.Build"/> shows each kind of caller, per the access rule the page declares.
/// </summary>
/// <remarks>
/// Why this is the test that matters for a public site: the registry gained an anonymous form so that a
/// sidebar built for a visitor with no session shows the public pages and nothing else. That is a statement
/// about NavTree's output, not about PageAccess in isolation.
/// </remarks>
public sealed class NavTreeAccessTests
{
    private sealed class TestPage : IPage
    {
        public TestPage(string name, IPageAccess access, INavItem navItem)
        {
            Name = name;
            Access = access;
            NavItem = navItem;
        }

        public string Name { get; }

        public Type Component => typeof(TestPage);

        public INavItem NavItem { get; }

        public IPageAccess Access { get; }
    }

    private static readonly Func<string, bool> HoldsNothing = _ => false;

    private static readonly Func<string, bool> HoldsConnectionsRead =
        p => string.Equals(p, "connections:read", StringComparison.Ordinal);

    private static IPage Visible(string name, IPageAccess access) =>
        new TestPage(name, access, new NavItem(name, "database", null, 0));

    private static IPage[] ThreePages() =>
    [
        Visible("Public", PageAccess.Anonymous),
        Visible("SignedIn", PageAccess.Authenticated),
        Visible("Gated", PageAccess.RequiringPermission("connections:read")),
    ];

    private static string[] NamesFor(bool isAuthenticated, Func<string, bool> hasPermission) =>
        NavTree.Build(ThreePages(), isAuthenticated, hasPermission)
            .SelectMany(g => g.Pages)
            .Select(p => p.Name)
            .ToArray();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Registration")]
    public void AnUnauthenticatedCallerSeesOnlyTheAnonymousPage()
    {
        NamesFor(isAuthenticated: false, HoldsNothing).ShouldBe(new[] { "Public" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Registration")]
    public void AnUnauthenticatedCallerSeesNoMoreWhenThePredicateIsPermissive()
    {
        NamesFor(isAuthenticated: false, _ => true).ShouldBe(new[] { "Public" });
    }

    [Fact]
    [Trait("Category", "Registration")]
    public void AnAuthenticatedCallerWithNoPermissionsSeesThePublicAndSignedInPages()
    {
        NamesFor(isAuthenticated: true, HoldsNothing)
            .ShouldBe(new[] { "Public", "SignedIn" }, ignoreOrder: true);
    }

    [Fact]
    [Trait("Category", "Registration")]
    public void AnAuthenticatedCallerHoldingThePermissionSeesAllThree()
    {
        NamesFor(isAuthenticated: true, HoldsConnectionsRead)
            .ShouldBe(new[] { "Public", "SignedIn", "Gated" }, ignoreOrder: true);
    }

    [Fact]
    [Trait("Category", "Registration")]
    public void APageWithNoNavEntryIsExcludedEvenWhenAnyoneMayReachIt()
    {
        var hidden = new TestPage("Hidden", PageAccess.Anonymous, NavItem.Empty);

        NavTree.Build([hidden], isAuthenticated: false, HoldsNothing).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Category", "Registration")]
    public void BuildStillRequiresAPermissionPredicate()
    {
        Should.Throw<ArgumentNullException>(() => NavTree.Build(ThreePages(), isAuthenticated: true, null!));
    }
}
