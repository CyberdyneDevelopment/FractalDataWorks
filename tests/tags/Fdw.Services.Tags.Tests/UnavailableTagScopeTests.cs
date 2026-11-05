using System.Threading.Tasks;
using System;
using Fdw.Services.Tags.Abstractions;
using Fdw.Services.Tags.Configuration;
using Fdw.Services.Tags.Results;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tags.Tests;

/// <summary>A host with no workspace provider refuses to put a tag on a thing, and says why (FDW-3816).</summary>
[Trait("Category", "Tags")]
public sealed class UnavailableTagScopeTests
{
    [Theory]
    [InlineData("Tenant")]
    [InlineData("Organization")]
    [InlineData("Workspace")]
    [Trait("Priority", "P0")]
    public async Task EveryTagIsRefusedWhateverItsOwner(string ownerKind)
    {
        var tag = new TagDomainConfiguration { Id = Guid.NewGuid(), Name = "finance", OwnerKind = ownerKind, OwnerId = Guid.NewGuid() };

        var allowed = await new UnavailableTagScope().Allows(tag, TaggableKinds.ByName("Pipeline"), Guid.NewGuid(), TestContext.Current.CancellationToken);

        allowed.IsFailure.ShouldBeTrue();
        allowed.Code.ShouldBeOfType<TagScopeUnavailableCode>();
    }
}
