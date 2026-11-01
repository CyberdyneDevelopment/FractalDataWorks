using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Workspaces.Endpoints;
using Fdw.Services.Workspaces.Results;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces.Map;

/// <summary>FDW-1029: a node says it can be edited exactly when the caller holds the write permission of the endpoint that edits it.</summary>
[Collection(nameof(ServicesTestCollection))]
public class MapEditingTests
{
    internal static readonly IAuthenticationContext Caller = new ClaimsPrincipalAuthenticationContext(new ClaimsPrincipal(new ClaimsIdentity("test")));

    /// <summary>An authorization that grants exactly the named "resource:action" permissions.</summary>
    internal static Mock<IFrameworkAuthorizationService> Authorization(params string[] granted)
    {
        var authorization = new Mock<IFrameworkAuthorizationService>();
        authorization
            .Setup(a => a.Authorize(It.IsAny<IAuthenticationContext>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IAuthenticationContext _, string resource, string action, CancellationToken _) =>
                GenericResult<bool>.Success(granted.Contains($"{resource}:{action}", StringComparer.Ordinal)));
        return authorization;
    }

    private static MapNodeDto Node(string kind) => new() { Id = kind, Kind = kind };

    private static async Task<MapEditing> Resolve(Mock<IFrameworkAuthorizationService> authorization, bool mayEditWorkspace = false)
    {
        var editing = await MapEditing.Resolve(authorization.Object, Caller, mayEditWorkspace, CancellationToken.None);
        editing.IsSuccess.ShouldBeTrue();
        return editing.Value!;
    }

    [Fact]
    public async Task ANodeIsEditableOnlyWhenTheCallerHoldsTheWritePermissionOfItsKind()
    {
        var editing = await Resolve(Authorization("datasets:write"));

        editing.Apply(Node(MapKinds.DataSet)).CanEdit.ShouldBeTrue();
        editing.Apply(Node(MapKinds.DataStore)).CanEdit.ShouldBeFalse();
        editing.Apply(Node(MapKinds.Pipeline)).CanEdit.ShouldBeFalse();
        editing.Apply(Node(MapKinds.Container)).CanEdit.ShouldBeFalse();
    }

    [Fact]
    public async Task AContainerIsEditedInsideItsDataStore()
    {
        var editing = await Resolve(Authorization("datastores:write"));

        editing.Apply(Node(MapKinds.DataStore)).CanEdit.ShouldBeTrue();
        editing.Apply(Node(MapKinds.Container)).CanEdit.ShouldBeTrue();
        editing.Apply(Node(MapKinds.DataSet)).CanEdit.ShouldBeFalse();
    }

    [Fact]
    public async Task AKindTheMapDoesNotKnowIsNeverEditableFromTheMap()
    {
        var editing = await Resolve(Authorization("datasets:write", "datastores:write", "pipelines:write", "data-views:write"), mayEditWorkspace: true);

        editing.Apply(Node("DataView")).CanEdit.ShouldBeFalse();
    }

    [Fact]
    public async Task EachResourceIsAskedOnceHoweverManyKindsShareIt()
    {
        var authorization = Authorization("datasets:write", "datastores:write", "pipelines:write");

        await Resolve(authorization);

        authorization.Verify(a => a.Authorize(It.IsAny<IAuthenticationContext>(), "datastores", "write", It.IsAny<CancellationToken>()), Times.Once);
        authorization.Verify(a => a.Authorize(It.IsAny<IAuthenticationContext>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task AnAuthorizationThatCannotAnswerFailsTheResolveInsteadOfReadingAsNo()
    {
        var authorization = new Mock<IFrameworkAuthorizationService>();
        authorization
            .Setup(a => a.Authorize(It.IsAny<IAuthenticationContext>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<bool>.Failure(
                WorkspacesResultCodes.ByName("MapRequestInvalid"), ResultDetails.Create("reason", "the authorization could not answer")));

        var editing = await MapEditing.Resolve(authorization.Object, Caller, mayEditWorkspace: false, CancellationToken.None);

        editing.IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData(true, MapRelations.RelatesTo, true)]
    [InlineData(false, MapRelations.RelatesTo, false)]
    [InlineData(true, MapRelations.StoredIn, false)]
    [InlineData(true, MapRelations.DerivedFrom, false)]
    [InlineData(true, MapRelations.ConsumedBy, false)]
    public async Task OnlyAJoinAWorkspaceDeclaresIsEditableAndOnlyByWhoMayEditTheWorkspace(bool mayEditWorkspace, string relationType, bool expected)
    {
        var editing = await Resolve(Authorization(), mayEditWorkspace);

        editing.Apply(new MapEdgeDto { Id = "e", RelationType = relationType }).CanEdit.ShouldBe(expected);
    }
}
