using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Workspaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces;

/// <summary>
/// FDW-2560: the place a definition is read or written for comes from who is signed in, the places above it are those of the caller's own tenant, and the
/// scope each is judged at names its tenant and the organization it is or sits in.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public class CallerPlacesTests
{
    private static readonly Guid Tenant = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid OtherTenant = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid Finance = Guid.Parse("44444444-0000-0000-0000-000000000001");
    private static readonly Guid Desk = Guid.Parse("66666666-0000-0000-0000-000000000001");

    private static IAuthenticationContextAccessor Caller(bool signedIn, Guid? tenant, Guid? organization)
    {
        var context = new Mock<IAuthenticationContext>();
        context.SetupGet(c => c.IsAuthenticated).Returns(signedIn);
        context.SetupGet(c => c.ActiveTenantId).Returns(tenant);
        context.SetupGet(c => c.ActiveOrgId).Returns(organization);
        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupGet(a => a.Current).Returns(context.Object);
        return accessor.Object;
    }

    private static IPlaceAncestry Ancestry(string kind, Guid id, params PlaceStep[] steps)
    {
        var ancestry = new Mock<IPlaceAncestry>();
        ancestry
            .Setup(a => a.Get(kind, id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<PlaceStep>>.Success(steps));
        return ancestry.Object;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void TheCallersPlaceIsTheNamedWorkspaceElseTheirOrganizationElseTheirTenant()
    {
        CallerPlaces.Of(Caller(true, Tenant, Finance), Desk).ShouldBe(new CallerPlace(PlaceKinds.Workspace, Desk, Tenant));
        CallerPlaces.Of(Caller(true, Tenant, Finance), null).ShouldBe(new CallerPlace(PlaceKinds.Organization, Finance, Tenant));
        CallerPlaces.Of(Caller(true, Tenant, null), null).ShouldBe(new CallerPlace(PlaceKinds.Tenant, Tenant, Tenant));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ACallerWhoIsNotSignedInOrActsInNoTenantHasNoPlace()
    {
        CallerPlaces.Of(Caller(false, Tenant, Finance), null).ShouldBeNull();
        CallerPlaces.Of(Caller(true, null, Finance), Desk).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task APlaceInTheCallersOwnTenantIsReadAndOneInAnotherTenantIsRefused()
    {
        var steps = new[] { new PlaceStep(PlaceKinds.Organization, Finance, 0), new PlaceStep(PlaceKinds.Tenant, Tenant, 1) };
        var ancestry = Ancestry(PlaceKinds.Organization, Finance, steps);

        var own = await CallerPlaces.Above(ancestry, new CallerPlace(PlaceKinds.Organization, Finance, Tenant), NullLogger.Instance, TestContext.Current.CancellationToken);
        var other = await CallerPlaces.Above(ancestry, new CallerPlace(PlaceKinds.Organization, Finance, OtherTenant), NullLogger.Instance, TestContext.Current.CancellationToken);

        own.IsSuccess.ShouldBeTrue();
        own.Value.ShouldBe(steps);
        other.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void AStepIsJudgedAtItsTenantAndTheOrganizationItIsOrSitsIn()
    {
        var steps = new[]
        {
            new PlaceStep(PlaceKinds.Workspace, Desk, 0), new PlaceStep(PlaceKinds.Organization, Finance, 1), new PlaceStep(PlaceKinds.Tenant, Tenant, 2),
        };

        CallerPlaces.ScopeOf(steps, steps[0])!.OrganizationId.ShouldBe(Finance);
        CallerPlaces.ScopeOf(steps, steps[1])!.OrganizationId.ShouldBe(Finance);
        CallerPlaces.ScopeOf(steps, steps[2])!.OrganizationId.ShouldBeNull();
        CallerPlaces.ScopeOf(steps, steps[2])!.TenantId.ShouldBe(Tenant);
        CallerPlaces.ScopeOf([new PlaceStep(PlaceKinds.Workspace, Desk, 0)], steps[0]).ShouldBeNull();
    }
}
