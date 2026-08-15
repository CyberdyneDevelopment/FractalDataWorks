using Fdw.DevSession.Sessions;

namespace Fdw.DevSession.Tests;

/// <summary>The path-overlap rule that strand fencing is built on.</summary>
public sealed class ScopePathsTests
{
    [Theory]
    [InlineData("src/Foo.cs", "src/Foo.cs")]                 // identical
    [InlineData("src", "src/Foo.cs")]                        // ancestor directory
    [InlineData("src/Foo.cs", "src")]                        // descendant, order reversed
    [InlineData("src/Foo.cs", "SRC/FOO.CS")]                 // case-insensitive
    [InlineData("src\\Foo.cs", "src/Foo.cs")]                // separator-insensitive
    [InlineData("src/", "src/Foo.cs")]                       // trailing separator
    public void Overlapping_paths_are_detected(string left, string right)
        => ScopePaths.Overlap([left], [right]).ShouldBeTrue();

    [Theory]
    [InlineData("src/Foo.cs", "src/Bar.cs")]                 // siblings
    [InlineData("src/Foo", "src/FooBar")]                    // shared prefix, NOT an ancestor
    [InlineData("src", "tests")]                             // unrelated roots
    [InlineData("src/a/b", "src/a/c")]                       // sibling subtrees
    public void Non_overlapping_paths_are_allowed(string left, string right)
        => ScopePaths.Overlap([left], [right]).ShouldBeFalse();

    [Fact]
    public void Overlap_is_detected_across_multi_path_claims()
    {
        // Why: a claim is a SET. One colliding member is enough to refuse the whole claim.
        ScopePaths.Overlap(["docs", "src/Foo.cs"], ["tests", "src/Foo.cs"]).ShouldBeTrue();
        ScopePaths.Overlap(["docs", "src/Foo.cs"], ["tests", "src/Bar.cs"]).ShouldBeFalse();
    }

    [Fact]
    public void Normalize_rejects_an_empty_path()
        => Should.Throw<System.ArgumentException>(() => ScopePaths.Normalize("  "));
}
