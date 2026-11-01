using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Workspaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces;

/// <summary>
/// FDW-2538: the places above a place are found through the configuration that holds them. A workspace names its organization, or its tenant when it
/// names none; an organization names its tenant; the tenant is the top. A place that does not exist, a kind that is not known and a read that fails are
/// failures, never an empty answer.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public class PlaceAncestryTests
{
    private static readonly Guid Tenant = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Finance = Guid.Parse("44444444-0000-0000-0000-000000000001");
    private static readonly Guid Workspace = Guid.Parse("66666666-0000-0000-0000-000000000001");

    private sealed class Fixture
    {
        public Mock<ITenantProvider> Tenants { get; } = new();

        public Mock<IOrganizationProvider> Organizations { get; } = new();

        public Mock<IWorkspaceConfigurationProvider> Workspaces { get; } = new();

        public PlaceAncestry Ancestry => new(
            [
                new TenantPlaceParentSource(Tenants.Object, NullLogger<TenantPlaceParentSource>.Instance),
                new OrganizationPlaceParentSource(Organizations.Object, NullLogger<OrganizationPlaceParentSource>.Instance),
                new WorkspacePlaceParentSource(Workspaces.Object, NullLogger<WorkspacePlaceParentSource>.Instance),
            ],
            NullLogger<PlaceAncestry>.Instance);
    }

    private static Fixture Holding(
        bool tenant = true, bool organization = true, bool deletedOrganization = false, bool workspace = true, Guid? workspaceOrganization = null,
        Guid? workspaceTenant = null)
    {
        var fixture = new Fixture();
        fixture.Tenants.Setup(t => t.GetTenant(Tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant ? GenericResult<ITenant>.Success(new Mock<ITenant>().Object) : GenericResult<ITenant>.Success(default!));
        fixture.Organizations.Setup(o => o.Get(Finance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization
                ? GenericResult<OrganizationConfiguration>.Success(new OrganizationConfiguration { Id = Finance, TenantId = Tenant, IsDeleted = deletedOrganization })
                : GenericResult<OrganizationConfiguration>.Success(default!));
        var held = new Mock<IWorkspaceImplementationConfiguration>();
        held.SetupGet(w => w.OrganizationId).Returns(workspaceOrganization);
        held.SetupGet(w => w.TenantId).Returns(workspaceTenant);
        fixture.Workspaces.Setup(w => w.Get(Workspace, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace
                ? GenericResult<IWorkspaceImplementationConfiguration>.Success(held.Object)
                : GenericResult<IWorkspaceImplementationConfiguration>.Success(default!));
        return fixture;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AWorkspaceInAnOrganizationListsItselfItsOrganizationAndItsTenantNearestFirst()
    {
        var steps = await Holding(workspaceOrganization: Finance, workspaceTenant: Tenant).Ancestry.Get(PlaceKinds.Workspace, Workspace);

        steps.IsSuccess.ShouldBeTrue();
        steps.Value!.ToArray().ShouldBe(new[]
        {
            new PlaceStep(PlaceKinds.Workspace, Workspace, 0),
            new PlaceStep(PlaceKinds.Organization, Finance, 1),
            new PlaceStep(PlaceKinds.Tenant, Tenant, 2),
        });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AWorkspaceThatNamesNoOrganizationListsItselfAndItsTenant()
    {
        var steps = await Holding(workspaceTenant: Tenant).Ancestry.Get(PlaceKinds.Workspace, Workspace);

        steps.Value!.ToArray().ShouldBe(new[]
        {
            new PlaceStep(PlaceKinds.Workspace, Workspace, 0),
            new PlaceStep(PlaceKinds.Tenant, Tenant, 1),
        });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AnOrganizationListsItselfAndItsTenant()
    {
        var steps = await Holding().Ancestry.Get(PlaceKinds.Organization, Finance);

        steps.Value!.ToArray().ShouldBe(new[]
        {
            new PlaceStep(PlaceKinds.Organization, Finance, 0),
            new PlaceStep(PlaceKinds.Tenant, Tenant, 1),
        });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ATenantListsOnlyItself()
    {
        var steps = await Holding().Ancestry.Get(PlaceKinds.Tenant, Tenant);

        steps.Value!.ToArray().ShouldBe(new[] { new PlaceStep(PlaceKinds.Tenant, Tenant, 0) });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AWorkspaceThatDoesNotExistIsAFailureAndNotAnEmptyList()
    {
        var steps = await Holding(workspace: false).Ancestry.Get(PlaceKinds.Workspace, Workspace);

        steps.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ADeletedOrganizationIsNotAPlace()
    {
        var steps = await Holding(deletedOrganization: true).Ancestry.Get(PlaceKinds.Organization, Finance);

        steps.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ATenantThatDoesNotExistIsNotAPlace()
    {
        var steps = await Holding(tenant: false).Ancestry.Get(PlaceKinds.Tenant, Tenant);

        steps.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AWorkspaceThatNamesNeitherAnOrganizationNorATenantSitsNowhereAndIsAFailure()
    {
        var steps = await Holding().Ancestry.Get(PlaceKinds.Workspace, Workspace);

        steps.IsFailure.ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("Department")]
    public async Task AKindThatIsNotKnownIsAFailureAndNothingIsRead(string kind)
    {
        var fixture = Holding();

        var steps = await fixture.Ancestry.Get(kind, Tenant);

        steps.IsFailure.ShouldBeTrue();
        fixture.Tenants.Verify(t => t.GetTenant(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AReadThatFailsIsAFailureAndNotAnEmptyList()
    {
        var fixture = Holding();
        fixture.Workspaces.Setup(w => w.Get(Workspace, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IWorkspaceImplementationConfiguration>.Failure(new GenericMessage("the configuration store is down")));

        var steps = await fixture.Ancestry.Get(PlaceKinds.Workspace, Workspace);

        steps.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AWalkThatNeverReachesATopIsAFailureAndDoesNotLoop()
    {
        var one = new Mock<IPlaceParentSource>();
        one.SetupGet(s => s.Kind).Returns("One");
        one.Setup(s => s.Above(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<PlaceRef?>.Success(new PlaceRef("Two", Tenant)));
        var two = new Mock<IPlaceParentSource>();
        two.SetupGet(s => s.Kind).Returns("Two");
        two.Setup(s => s.Above(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<PlaceRef?>.Success(new PlaceRef("One", Tenant)));

        var steps = await new PlaceAncestry([one.Object, two.Object], NullLogger<PlaceAncestry>.Instance).Get("One", Tenant);

        steps.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TwoSourcesForOneKindAreRefused()
    {
        var first = new TenantPlaceParentSource(new Mock<ITenantProvider>().Object, NullLogger<TenantPlaceParentSource>.Instance);
        var second = new TenantPlaceParentSource(new Mock<ITenantProvider>().Object, NullLogger<TenantPlaceParentSource>.Instance);

        Should.Throw<ArgumentException>(() => new PlaceAncestry([first, second], NullLogger<PlaceAncestry>.Instance));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task TheDefaultPlaceServiceTypeRegistersTheAncestryAndOneSourceForEachKindAndNoneTwice()
    {
        var fixture = Holding(workspaceOrganization: Finance, workspaceTenant: Tenant);
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Services.AddSingleton(fixture.Tenants.Object);
        builder.Services.AddSingleton(fixture.Organizations.Object);
        builder.Services.AddSingleton(fixture.Workspaces.Object);
        builder.Services.AddLogging();
        var option = new DefaultPlaceServiceType();

        option.Register(builder, NullLoggerFactory.Instance).IsSuccess.ShouldBeTrue();
        option.Register(builder, NullLoggerFactory.Instance).IsSuccess.ShouldBeTrue();

        using var provider = builder.Services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetServices<IPlaceParentSource>().Select(source => source.Kind).Order(StringComparer.Ordinal).ToArray()
            .ShouldBe(new[] { PlaceKinds.Organization, PlaceKinds.Tenant, PlaceKinds.Workspace });
        var steps = await scope.ServiceProvider.GetRequiredService<IPlaceAncestry>().Get(PlaceKinds.Workspace, Workspace);
        steps.Value!.Select(step => step.Kind).ToArray().ShouldBe(new[] { PlaceKinds.Workspace, PlaceKinds.Organization, PlaceKinds.Tenant });
    }
}
