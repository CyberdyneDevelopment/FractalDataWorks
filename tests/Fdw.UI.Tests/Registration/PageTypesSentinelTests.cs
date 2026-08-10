using Fdw.UI.Navigation;
using Shouldly;
using Xunit;

namespace Fdw.UI.Tests.Registration;

/// <summary>
/// Pins the one thing that must hold before any UI app can start: touching <see cref="PageTypes"/> must
/// not throw.
/// </summary>
/// <remarks>
/// Why this exists: PageTypeBase rejects an empty page list, which is right for a declared page group and
/// wrong for the collection's generated NotFound sentinel — the sentinel contributes nothing by
/// definition. When the sentinel fell through to the validating constructor it threw
/// "Page type '_Empty' declares no pages" from the static initializer of PageTypes. That runs inside a
/// module initializer, so EVERY app registering UI page types aborted before Main and crash-looped with
/// no route, no health endpoint and no obvious cause. reference-ui did exactly that on a preview slot.
///
/// A test project existed for this namespace but had no tests in it, so nothing touched the collection
/// and the failure only surfaced at deploy time.
/// </remarks>
public sealed class PageTypesSentinelTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Registration")]
    public void TouchingTheCollectionDoesNotThrow()
    {
        // Why Should.NotThrow rather than asserting a value: the defect was a TypeInitializationException
        // from the static constructor, so merely forcing the type to initialize is the assertion.
        Should.NotThrow(() => PageTypes.All());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Registration")]
    public void NotFoundSentinelResolvesAndDeclaresNoPages()
    {
        var sentinel = PageTypes.NotFound;

        sentinel.ShouldNotBeNull();
        // The sentinel is the collection's "no such page type" answer — contributing nothing is precisely
        // what it means, and constructing it must not trip the declared-group validation.
        sentinel.Pages.ShouldBeEmpty();
        sentinel.PageAssemblies.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Registration")]
    public void UnknownNameReturnsTheSentinelRatherThanThrowing()
    {
        // Why: ByName returns the NotFound sentinel for a miss (never null), so a caller compares against
        // NotFound instead of null-checking. That contract only works if the sentinel can be constructed.
        PageTypes.ByName("no-such-page-type").ShouldBe(PageTypes.NotFound);
    }
}
