using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Tags.Abstractions;
using Fdw.Services.Tags.Configuration;
using Fdw.Services.Workspaces;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tags.Tests;

/// <summary>The scope rule: a tenant tag goes on anything; an organization or workspace tag only on a thing placed under its owner.</summary>
[Trait("Category", "Tags")]
public sealed class TagScopeTests
{
    private static readonly Guid Thing = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid Workspace = Guid.Parse("00000000-0000-0000-0000-0000000000b1");
    private static readonly Guid Organization = Guid.Parse("00000000-0000-0000-0000-0000000000c1");

    private static TagDomainConfiguration Tag(string ownerKind, Guid ownerId)
        => new() { Id = Guid.NewGuid(), Name = "finance", OwnerKind = ownerKind, OwnerId = ownerId };

    private static WorkspaceImplementationConfiguration Holding(Guid organizationId, string resourceType, Guid resourceId)
        => new() { OrganizationId = organizationId, Resources = [new WorkspaceResourceConfiguration { ResourceType = resourceType, ResourceId = resourceId }] };

    private static TagScope ScopeOver(params WorkspaceImplementationConfiguration[] workspaces)
    {
        var provider = new Mock<IWorkspaceConfigurationProvider>();
        provider
            .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IWorkspaceImplementationConfiguration>.Success(workspaces.Length == 0 ? new WorkspaceImplementationConfiguration() : workspaces[0]));
        provider
            .Setup(p => p.Get(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IWorkspaceImplementationConfiguration>>.Success([.. workspaces]));
        return new TagScope(provider.Object);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ATenantTagMayGoOnAnyKind()
    {
        var allowed = await ScopeOver().Allows(Tag(TagOwnerKinds.Tenant.Name, Guid.NewGuid()), TaggableKinds.ByName("Connection"), Thing, TestContext.Current.CancellationToken);

        allowed.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Organization")]
    [InlineData("Workspace")]
    [Trait("Priority", "P0")]
    public async Task AnOrganizationOrWorkspaceTagMayNotGoOnATenantWideKind(string ownerKind)
    {
        var refused = await ScopeOver().Allows(Tag(ownerKind, Organization), TaggableKinds.ByName("Schedule"), Thing, TestContext.Current.CancellationToken);

        refused.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AWorkspaceTagGoesOnAThingThatWorkspaceHolds()
    {
        var scope = ScopeOver(Holding(Organization, "DataSet", Thing));

        var allowed = await scope.Allows(Tag(TagOwnerKinds.Workspace.Name, Workspace), TaggableKinds.ByName("DataSet"), Thing, TestContext.Current.CancellationToken);

        allowed.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AWorkspaceTagDoesNotGoOnAThingThatWorkspaceDoesNotHold()
    {
        var scope = ScopeOver(Holding(Organization, "DataSet", Guid.NewGuid()));

        var refused = await scope.Allows(Tag(TagOwnerKinds.Workspace.Name, Workspace), TaggableKinds.ByName("DataSet"), Thing, TestContext.Current.CancellationToken);

        refused.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnOrganizationTagGoesOnAThingHeldByOneOfItsWorkspaces()
    {
        var scope = ScopeOver(Holding(Guid.NewGuid(), "Pipeline", Thing), Holding(Organization, "Pipeline", Thing));

        var allowed = await scope.Allows(Tag(TagOwnerKinds.Organization.Name, Organization), TaggableKinds.ByName("Pipeline"), Thing, TestContext.Current.CancellationToken);

        allowed.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnOrganizationTagDoesNotGoOnAThingHeldOnlyByAnotherOrganization()
    {
        var scope = ScopeOver(Holding(Guid.NewGuid(), "Pipeline", Thing));

        var refused = await scope.Allows(Tag(TagOwnerKinds.Organization.Name, Organization), TaggableKinds.ByName("Pipeline"), Thing, TestContext.Current.CancellationToken);

        refused.IsFailure.ShouldBeTrue();
    }
}
