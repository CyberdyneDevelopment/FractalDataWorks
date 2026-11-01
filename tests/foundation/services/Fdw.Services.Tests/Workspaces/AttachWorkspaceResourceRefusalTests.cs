using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Workspaces.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces;

/// <summary>
/// FDW-1053: attaching a resource to a workspace says why it was refused. A kind nobody registered is refused with the kinds
/// that are; a kind that is registered but has no label resolver is refused as not attachable, not as not registered.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public class AttachWorkspaceResourceRefusalTests
{
    [Fact]
    public async Task AnUnregisteredKindIsRefusedWithTheRegisteredKinds()
    {
        var result = await new Probe().Attach("Quakes", "Spreadsheet");

        result.IsFailure.ShouldBeTrue();
        result.Code!.Name.ShouldBe("WorkspaceResourceKindUnknown");
        var shown = result.Code.FormatClientMessage(result.Details).ShouldNotBeNull();
        shown.ShouldContain("resourceType 'Spreadsheet'");
        shown.ShouldContain("DataView");
    }

    [Fact]
    public async Task ARegisteredKindWithNoLabelResolverIsRefusedAsNotAttachable()
    {
        var result = await new Probe().Label("Quakes", "Pipeline");

        result.IsFailure.ShouldBeTrue();
        result.Code!.Name.ShouldBe("WorkspaceResourceKindNotAttachable");
        result.Code.FormatClientMessage(result.Details)
            .ShouldBe("Workspace 'Quakes' rejected: a Pipeline cannot be attached yet; its kind is registered but no label resolver is wired for it");
    }

    private sealed class Probe : AttachWorkspaceResourceEndpointBase
    {
        // The two paths under test refuse before they touch a provider, so none is supplied.
        public Probe()
            : base(NullLogger<AttachWorkspaceResourceEndpointBase>.Instance, null!, null!, null!, null!, null!)
        {
        }

        public Task<IGenericResult<WorkspaceMapNodeDto>> Attach(string workspace, string kind)
            => Create(new AttachWorkspaceResourceRequest { Name = workspace, ResourceType = kind, ResourceId = Guid.NewGuid(), Relationship = "Uses" }, CancellationToken.None);

        public Task<IGenericResult<string>> Label(string workspace, string kind)
            => ResolveResourceLabel(workspace, kind, Guid.NewGuid(), CancellationToken.None);
    }
}
