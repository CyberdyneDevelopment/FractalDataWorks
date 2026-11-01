using System;
using Fdw.Services.Authentication.Abstractions.Security;

namespace Fdw.Services.Authentication.Abstractions.Tests.Security;

/// <summary>
/// The one coverage rule every check of a held permission answers through (FDW-1082).
/// </summary>
public sealed class PermissionCoverageTests
{
    private static readonly Guid DataSetId = new("0f0f0f0f-0000-0000-0000-000000000001");

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData("datasets:read", "datasets:read")]
    [InlineData("datasets:*", "datasets:read")]
    [InlineData("DATASETS:READ", "datasets:read")]
    public void CoversTheExactNameAndTheResourceWildcard(string held, string required)
        => PermissionCoverage.Covers([held], required).ShouldBeTrue();

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData("datasets:write", "datasets:read")]
    [InlineData("pipelines:*", "datasets:read")]
    [InlineData("datasets:read:extra", "datasets:read")]
    public void DoesNotCoverAnotherActionOrResource(string held, string required)
        => PermissionCoverage.Covers([held], required).ShouldBeFalse();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CoversAPerResourcePermissionByTheResourceWideGrants()
    {
        var required = $"datasets/{DataSetId:D}:grant";

        PermissionCoverage.Covers([required], required).ShouldBeTrue();
        PermissionCoverage.Covers([$"datasets/{DataSetId:D}:*"], required).ShouldBeTrue();
        PermissionCoverage.Covers(["datasets:grant"], required).ShouldBeTrue();
        PermissionCoverage.Covers(["datasets:*"], required).ShouldBeTrue();
    }

    // A held *:* covers every permission, a per-resource one included: every check answers through this one rule.
    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData("datasets:read")]
    [InlineData("settings/role:write")]
    [InlineData("datasets/0f0f0f0f-0000-0000-0000-000000000001:grant")]
    public void TheEverythingWildcardCoversEverything(string required)
        => PermissionCoverage.Covers(["*:*"], required).ShouldBeTrue();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void APerResourceGrantDoesNotCoverAnotherItemOrTheWholeResource()
    {
        var other = new Guid("0f0f0f0f-0000-0000-0000-000000000002");

        PermissionCoverage.Covers([$"datasets/{other:D}:grant"], $"datasets/{DataSetId:D}:grant").ShouldBeFalse();
        PermissionCoverage.Covers([$"datasets/{DataSetId:D}:grant"], "datasets:grant").ShouldBeFalse();
    }

    // A resource name with a slash whose last segment is not a Guid is a resource in its own right, not an item
    // of its first segment: settings:write must never cover settings/role:write.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ANestedResourceNameIsNeverWidenedToItsFirstSegment()
    {
        PermissionCoverage.Covers(["settings:write"], "settings/role:write").ShouldBeFalse();
        PermissionCoverage.Covers(["settings:*"], "settings/role:write").ShouldBeFalse();
        PermissionCoverage.Covers(["settings/role:*"], "settings/role:write").ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void NothingHeldCoversNothing()
        => PermissionCoverage.Covers([], "datasets:read").ShouldBeFalse();
}
