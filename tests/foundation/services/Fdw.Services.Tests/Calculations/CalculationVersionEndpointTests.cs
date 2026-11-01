using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Calculations.Endpoints.CalculationEntities;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Calculations;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Calculations;

/// <summary>
/// FDW-2560: a calculation has versions held at organizations and workspaces. They are listed nearest the caller first, the calculation itself is the
/// tenant's version, a place the caller may not read is absent, and a version is saved or deleted only at a place of the caller's tenant where the write
/// right is held.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public class CalculationVersionEndpointTests
{
    private static readonly Guid Tenant = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid OtherTenant = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid Finance = Guid.Parse("44444444-0000-0000-0000-000000000001");
    private static readonly Guid Legal = Guid.Parse("44444444-0000-0000-0000-000000000002");
    private static readonly Guid Workspace = Guid.Parse("66666666-0000-0000-0000-000000000001");
    private static readonly Guid CalculationId = Guid.Parse("77777777-0000-0000-0000-000000000002");

    private sealed class ListVersions(
        ICalculationConfigurationProvider calculations, ICalculationVersionConfigurationProvider versions, IPlaceAncestry ancestry, IScopeAuthority authority, IAuthenticationContextAccessor context, IPlaceNames names)
        : ListCalculationVersionsEndpointBase(calculations, versions, ancestry, authority, context, names);

    private sealed class SaveVersion(
        ICalculationConfigurationProvider calculations, ICalculationVersionConfigurationProvider versions, IPlaceAncestry ancestry, IScopeAuthority authority, IAuthenticationContextAccessor context)
        : SaveCalculationVersionEndpointBase(calculations, versions, ancestry, authority, context);

    private sealed class DeleteVersion(
        ICalculationConfigurationProvider calculations, ICalculationVersionConfigurationProvider versions, IPlaceAncestry ancestry, IScopeAuthority authority, IAuthenticationContextAccessor context)
        : DeleteCalculationVersionEndpointBase(calculations, versions, ancestry, authority, context);

    private sealed class Rig
    {
        public Mock<ICalculationConfigurationProvider> Calculations { get; } = new();

        public Mock<ICalculationVersionConfigurationProvider> Versions { get; } = new();

        public Mock<IPlaceAncestry> Ancestry { get; } = new();

        public Mock<IScopeAuthority> Authority { get; } = new();

        public Mock<IAuthenticationContextAccessor> Context { get; } = new();

        public Mock<IPlaceNames> Names { get; } = new();

        public ICalculationVersionImplementationConfiguration? Saved { get; set; }

        public List<string> Deleted { get; } = [];
    }

    private static ICalculationEntityImplementationConfiguration Calculation()
    {
        var calculation = new Mock<ICalculationEntityImplementationConfiguration>();
        calculation.SetupGet(c => c.Id).Returns(CalculationId);
        calculation.SetupGet(c => c.Name).Returns("NetRevenue");
        calculation.SetupGet(c => c.Description).Returns("Base description");
        return calculation.Object;
    }

    private static async IAsyncEnumerable<T> Stream<T>(IEnumerable<T> rows)
    {
        foreach (var row in rows)
        {
            yield return row;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static CalculationVersionDomainConfiguration Version(string kind, Guid place, string description) => new()
    {
        Id = Guid.NewGuid(),
        Name = $"NetRevenue@{kind}:{place:D}",
        BaseCalculationId = CalculationId,
        CalculationName = "NetRevenue",
        ScopeKind = kind,
        ScopeId = place,
        Description = description,
    };

    private static Rig Create(
        bool calculationExists = true,
        IReadOnlyList<CalculationVersionDomainConfiguration>? versions = null,
        Func<ThingScope, bool>? may = null,
        bool signedIn = true,
        Guid? organization = null)
    {
        var setup = new Rig();
        var held = versions ?? [];
        var allowed = may ?? (_ => true);

        setup.Calculations.Setup(c => c.Get(CalculationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(calculationExists
                ? GenericResult<ICalculationEntityImplementationConfiguration>.Success(Calculation())
                : GenericResult<ICalculationEntityImplementationConfiguration>.Success(default!));

        setup.Versions.Setup(v => v.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainQuery query, CancellationToken _) => GenericResult<ConfigurationPage<CalculationVersionDomainConfiguration>>.Success(
                new ConfigurationPage<CalculationVersionDomainConfiguration>(query.Skip, query.Take, false, _ => Stream(held))));
        setup.Versions.Setup(v => v.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, CancellationToken _) => GenericResult<ICalculationVersionImplementationConfiguration>.Success(
                Existing(held.FirstOrDefault(row => row.Name == name))!));
        setup.Versions.Setup(v => v.Save(It.IsAny<ICalculationVersionImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((ICalculationVersionImplementationConfiguration saved, string _, string _, string _, CancellationToken _) => setup.Saved = saved)
            .ReturnsAsync(GenericResult.Success());
        setup.Versions.Setup(v => v.Delete(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string name, CancellationToken _) => setup.Deleted.Add(name))
            .ReturnsAsync(GenericResult.Success());

        setup.Ancestry.Setup(a => a.Get(PlaceKinds.Organization, Finance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<PlaceStep>>.Success([new PlaceStep(PlaceKinds.Organization, Finance, 0), new PlaceStep(PlaceKinds.Tenant, Tenant, 1)]));
        setup.Ancestry.Setup(a => a.Get(PlaceKinds.Workspace, Workspace, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<PlaceStep>>.Success(
                [new PlaceStep(PlaceKinds.Workspace, Workspace, 0), new PlaceStep(PlaceKinds.Organization, Finance, 1), new PlaceStep(PlaceKinds.Tenant, Tenant, 2)]));
        setup.Ancestry.Setup(a => a.Get(PlaceKinds.Tenant, Tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<PlaceStep>>.Success([new PlaceStep(PlaceKinds.Tenant, Tenant, 0)]));
        setup.Ancestry.Setup(a => a.Get(PlaceKinds.Organization, Legal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<PlaceStep>>.Success([new PlaceStep(PlaceKinds.Organization, Legal, 0), new PlaceStep(PlaceKinds.Tenant, OtherTenant, 1)]));

        setup.Authority.Setup(a => a.May(It.IsAny<string>(), It.IsAny<ThingScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, ThingScope scope, CancellationToken _) => allowed(scope)
                ? GenericResult.Success()
                : GenericResult.Failure(new GenericMessage("the caller does not hold the right there")));

        setup.Names.Setup(n => n.Get(PlaceKinds.Organization, Finance, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<string>.Success("Finance"));
        setup.Names.Setup(n => n.Get(PlaceKinds.Organization, Legal, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<string>.Success("Legal"));
        setup.Names.Setup(n => n.Get(PlaceKinds.Workspace, Workspace, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<string>.Success("Desk"));

        var context = new Mock<IAuthenticationContext>();
        context.SetupGet(c => c.IsAuthenticated).Returns(signedIn);
        context.SetupGet(c => c.ActiveTenantId).Returns(Tenant);
        context.SetupGet(c => c.ActiveOrgId).Returns(organization);
        setup.Context.SetupGet(c => c.Current).Returns(context.Object);
        return setup;
    }

    private static ICalculationVersionImplementationConfiguration? Existing(CalculationVersionDomainConfiguration? row)
        => row is null
            ? null
            : new CalculationVersionImplementationConfiguration
            {
                Id = row.Id, Name = row.Name, BaseCalculationId = row.BaseCalculationId, CalculationName = row.CalculationName,
                ScopeKind = row.ScopeKind, ScopeId = row.ScopeId, Description = row.Description,
            };

    private static SaveCalculationVersionRequest Request(string kind = "Organization", Guid? place = null, string description = "Finance description") => new()
    {
        Id = CalculationId, PlaceKind = kind, PlaceId = place ?? Finance, Description = description,
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AVersionAtTheCallersOrganizationAppliesAndTheCalculationItselfIsTheTenantsVersionBehindIt()
    {
        var setup = Create(versions: [Version("Organization", Finance, "Finance description")], organization: Finance);
        var endpoint = Factory.Create<ListVersions>(setup.Calculations.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object, setup.Names.Object);

        await endpoint.HandleAsync(new CalculationVersionsRequest { Id = CalculationId }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Name.ShouldBe("NetRevenue");
        endpoint.Response.Versions.Select(v => (v.PlaceKind, v.Depth, v.IsBase, v.Applies, v.Description)).ToArray().ShouldBe(new[]
        {
            ("Organization", 0, false, true, (string?)"Finance description"),
            ("Tenant", 1, true, false, (string?)"Base description"),
        });
        // The same name for each; the place's name follows it in parentheses, except for the tenant's own.
        endpoint.Response.Versions.Select(v => v.Label).ToArray().ShouldBe(new[] { "NetRevenue (Finance)", "NetRevenue" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AVersionHeldAtAnotherOrganizationIsNotListedSoTheCalculationItselfApplies()
    {
        var setup = Create(versions: [Version("Organization", Legal, "Legal description")], organization: Finance);
        var endpoint = Factory.Create<ListVersions>(setup.Calculations.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object, setup.Names.Object);

        await endpoint.HandleAsync(new CalculationVersionsRequest { Id = CalculationId }, CancellationToken.None);

        var only = endpoint.Response.Versions.ShouldHaveSingleItem();
        only.IsBase.ShouldBeTrue();
        only.Applies.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task APlaceTheCallerMayNotReadIsAbsentAndTheNextOneApplies()
    {
        var setup = Create(
            versions: [Version("Workspace", Workspace, "Workspace description"), Version("Organization", Finance, "Finance description")],
            may: scope => scope.OrganizationId is null);
        var endpoint = Factory.Create<ListVersions>(setup.Calculations.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object, setup.Names.Object);

        await endpoint.HandleAsync(new CalculationVersionsRequest { Id = CalculationId, WorkspaceId = Workspace }, CancellationToken.None);

        var only = endpoint.Response.Versions.ShouldHaveSingleItem();
        only.PlaceKind.ShouldBe("Tenant");
        only.Applies.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AWorkspaceOfAnotherTenantIsRefusedAsNotFound()
    {
        var setup = Create();
        setup.Ancestry.Setup(a => a.Get(PlaceKinds.Workspace, Workspace, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<PlaceStep>>.Success([new PlaceStep(PlaceKinds.Workspace, Workspace, 0), new PlaceStep(PlaceKinds.Tenant, OtherTenant, 1)]));
        var endpoint = Factory.Create<ListVersions>(setup.Calculations.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object, setup.Names.Object);

        await endpoint.HandleAsync(new CalculationVersionsRequest { Id = CalculationId, WorkspaceId = Workspace }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ACalculationThatDoesNotExistIsAnswered404()
    {
        var setup = Create(calculationExists: false, organization: Finance);
        var endpoint = Factory.Create<ListVersions>(setup.Calculations.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object, setup.Names.Object);

        await endpoint.HandleAsync(new CalculationVersionsRequest { Id = CalculationId }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ACallerWhoIsNotSignedInListsNothing()
    {
        var setup = Create(signedIn: false);
        var endpoint = Factory.Create<ListVersions>(setup.Calculations.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object, setup.Names.Object);

        await endpoint.HandleAsync(new CalculationVersionsRequest { Id = CalculationId }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(401);
        setup.Ancestry.Verify(a => a.Get(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ANewVersionIsSavedUnderTheCalculationsNameJoinedToThePlaceAndPointsAtTheCalculation()
    {
        var setup = Create();
        var endpoint = Factory.Create<SaveVersion>(setup.Calculations.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object);

        await endpoint.HandleAsync(Request(), CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        var saved = setup.Saved.ShouldNotBeNull();
        saved.Name.ShouldBe($"NetRevenue@Organization:{Finance:D}");
        saved.BaseCalculationId.ShouldBe(CalculationId);
        saved.ScopeKind.ShouldBe("Organization");
        saved.ScopeId.ShouldBe(Finance);
        saved.Description.ShouldBe("Finance description");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AVersionAlreadyHeldAtThePlaceIsReplacedAndKeepsItsIdentity()
    {
        var held = Version("Organization", Finance, "Old description");
        var setup = Create(versions: [held]);
        var endpoint = Factory.Create<SaveVersion>(setup.Calculations.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object);

        await endpoint.HandleAsync(Request(description: "New description"), CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        var saved = setup.Saved.ShouldNotBeNull();
        saved.Id.ShouldBe(held.Id);
        saved.Description.ShouldBe("New description");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AVersionCannotBeHeldAtTheTenantBecauseTheCalculationItselfIsTheTenantsVersion()
    {
        var setup = Create();
        var endpoint = Factory.Create<SaveVersion>(setup.Calculations.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object);

        await endpoint.HandleAsync(Request(kind: "Tenant", place: Tenant), CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(400);
        setup.Saved.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task APlaceOfAnotherTenantIsRefusedAsNotFoundAndNothingIsSaved()
    {
        var setup = Create();
        var endpoint = Factory.Create<SaveVersion>(setup.Calculations.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object);

        await endpoint.HandleAsync(Request(place: Legal), CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
        setup.Saved.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ACallerWithoutTheWriteRightAtThePlaceSavesNothing()
    {
        var setup = Create(may: _ => false);
        var endpoint = Factory.Create<SaveVersion>(setup.Calculations.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object);

        await endpoint.HandleAsync(Request(), CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(400);
        setup.Saved.ShouldBeNull();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AVersionWithNoDescriptionIsRefusedAs400(string description)
    {
        var setup = Create();
        var endpoint = Factory.Create<SaveVersion>(setup.Calculations.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object);

        await endpoint.HandleAsync(Request(description: description), CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(400);
        setup.Saved.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AVersionIsDeletedByTheNameItIsHeldUnderAndTheCalculationIsNotTouched()
    {
        var setup = Create(versions: [Version("Organization", Finance, "Finance description")]);
        var endpoint = Factory.Create<DeleteVersion>(setup.Calculations.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object);

        await endpoint.HandleAsync(new CalculationVersionPlaceRequest { Id = CalculationId, PlaceKind = "Organization", PlaceId = Finance }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        setup.Deleted.ShouldBe([$"NetRevenue@Organization:{Finance:D}"]);
        setup.Calculations.Verify(c => c.Delete(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task DeletingAVersionThatIsNotHeldIsAnswered404()
    {
        var setup = Create();
        var endpoint = Factory.Create<DeleteVersion>(setup.Calculations.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object);

        await endpoint.HandleAsync(new CalculationVersionPlaceRequest { Id = CalculationId, PlaceKind = "Organization", PlaceId = Finance }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
        setup.Deleted.ShouldBeEmpty();
    }
}
