using System.Linq;
using Fdw.Services.Tags.Abstractions;

namespace Fdw.Services.Tags.Tests;

/// <summary>Tests for the tag owner kinds (FDW-4100): the names are the values a tag row stores and the database check holds, so they are pinned.</summary>
public sealed class TagOwnerKindsTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Tags")]
    public void TheKindsAreExactlyTenantOrganizationAndWorkspace()
    {
        TagOwnerKinds.All().Select(kind => kind.Name).OrderBy(name => name, System.StringComparer.Ordinal)
            .ShouldBe(["Organization", "Tenant", "Workspace"]);
    }

    [Theory]
    [InlineData("Tenant")]
    [InlineData("Organization")]
    [InlineData("Workspace")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Tags")]
    public void AKindIsLookedUpByTheNameATagStores(string name)
    {
        ReferenceEquals(TagOwnerKinds.ByName(name), TagOwnerKinds.NotFound).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Tags")]
    public void ANameThatIsNotAKindAnswersTheNotFoundSentinel()
    {
        ReferenceEquals(TagOwnerKinds.ByName("Team"), TagOwnerKinds.NotFound).ShouldBeTrue();
    }
}
