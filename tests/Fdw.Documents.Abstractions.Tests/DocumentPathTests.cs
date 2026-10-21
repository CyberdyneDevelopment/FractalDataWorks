using Fdw.Documents.Abstractions;

namespace Fdw.Documents.Abstractions.Tests;

/// <summary>Tests for the document path and the scope check (FDW-1498): separators are normalised, no path climbs out of its scope, and containment is by whole segments.</summary>
public sealed class DocumentPathTests
{
    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData(@"reports\2026\q1.pdf", "reports/2026/q1.pdf")]
    [InlineData("/reports//2026/", "reports/2026")]
    [InlineData("a/b/c", "a/b/c")]
    public void SeparatorsAreNormalisedToOneForwardSlash(string text, string expected)
    {
        var parsed = DocumentPath.Parse(text);

        parsed.IsSuccess.ShouldBeTrue(parsed.CurrentMessage);
        parsed.Value.Value.ShouldBe(expected);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData("..")]
    [InlineData("a/../b")]
    [InlineData(@"a\..\..\secret")]
    [InlineData("./a")]
    public void AParentOrCurrentSegmentIsRefused(string text)
    {
        var parsed = DocumentPath.Parse(text);

        parsed.IsFailure.ShouldBeTrue();
        parsed.CurrentMessage.ShouldNotBeNull().ShouldContain("not a document path");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void NothingIsTheRootAndTheRootHasNoSegments()
    {
        DocumentPath.Parse(null).Value.IsRoot.ShouldBeTrue();
        DocumentPath.Parse(string.Empty).Value.IsRoot.ShouldBeTrue();
        DocumentPath.Root.Segments.ShouldBeEmpty();
        DocumentPath.Parse("a/b.pdf").Value.Name.ShouldBe("b.pdf");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ASiblingWhoseNameStartsTheSameIsNotWithin()
    {
        var folder = DocumentPath.Parse("a/b").Value;

        DocumentPath.Parse("a/b/c").Value.IsWithin(folder).ShouldBeTrue();
        DocumentPath.Parse("a/b").Value.IsWithin(folder).ShouldBeTrue();
        DocumentPath.Parse("a/bc").Value.IsWithin(folder).ShouldBeFalse();
        DocumentPath.Parse("a").Value.IsWithin(folder).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ARelativePathResolvesUnderTheRootOfTheScope()
    {
        var scope = new DocumentScope(DocumentPath.Parse("tenant-1/org-7").Value);

        var resolved = scope.Resolve(@"contracts\a.pdf");

        resolved.IsSuccess.ShouldBeTrue(resolved.CurrentMessage);
        resolved.Value.Value.ShouldBe("tenant-1/org-7/contracts/a.pdf");
        scope.Resolve(string.Empty).Value.Value.ShouldBe("tenant-1/org-7");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void APathThatClimbsOutOfTheScopeNeverResolves()
    {
        var scope = new DocumentScope(DocumentPath.Parse("tenant-1/org-7").Value);

        var resolved = scope.Resolve("../org-8/secret.pdf");

        resolved.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void APathTheStorageReportsOutsideTheScopeIsRefusedAndOneInsideComesBackRelative()
    {
        var scope = new DocumentScope(DocumentPath.Parse("tenant-1/org-7").Value);

        scope.Relative(DocumentPath.Parse("tenant-1/org-7/contracts/a.pdf").Value).Value.Value.ShouldBe("contracts/a.pdf");
        scope.Relative(DocumentPath.Parse("tenant-1/org-70/a.pdf").Value).IsFailure.ShouldBeTrue();
        scope.Contains(DocumentPath.Parse("tenant-2/x").Value).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AScopeAtTheRootOfTheAccountContainsEverything()
    {
        var scope = new DocumentScope(DocumentPath.Root);

        scope.Resolve("a/b").Value.Value.ShouldBe("a/b");
        scope.Relative(DocumentPath.Parse("a/b").Value).Value.Value.ShouldBe("a/b");
    }
}
