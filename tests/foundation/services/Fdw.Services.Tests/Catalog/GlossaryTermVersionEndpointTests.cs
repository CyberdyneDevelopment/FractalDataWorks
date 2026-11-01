using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Configuration;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Catalog;

/// <summary>
/// FDW-2331: a glossary term has versions held at organizations and workspaces. They are listed nearest the caller first, the term itself is the
/// tenant's version, a place the caller may not read is absent, and a version is saved or deleted only at a place of the caller's tenant where the
/// write right is held.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public class GlossaryTermVersionEndpointTests
{
    private static readonly Guid Tenant = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid OtherTenant = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid Finance = Guid.Parse("44444444-0000-0000-0000-000000000001");
    private static readonly Guid Legal = Guid.Parse("44444444-0000-0000-0000-000000000002");
    private static readonly Guid Workspace = Guid.Parse("66666666-0000-0000-0000-000000000001");
    private static readonly Guid TermId = Guid.Parse("77777777-0000-0000-0000-000000000001");

    private sealed class ListVersions(
        IGlossaryTermConfigurationProvider terms, IGlossaryTermVersionConfigurationProvider versions, IPlaceAncestry ancestry, IScopeAuthority authority, IAuthenticationContextAccessor context, IPlaceNames names)
        : ListGlossaryTermVersionsEndpointBase(terms, versions, ancestry, authority, context, names);

    private sealed class SaveVersion(
        IGlossaryTermConfigurationProvider terms, IGlossaryTermVersionConfigurationProvider versions, IPlaceAncestry ancestry, IScopeAuthority authority, IAuthenticationContextAccessor context)
        : SaveGlossaryTermVersionEndpointBase(terms, versions, ancestry, authority, context);

    private sealed class DeleteVersion(
        IGlossaryTermConfigurationProvider terms, IGlossaryTermVersionConfigurationProvider versions, IPlaceAncestry ancestry, IScopeAuthority authority, IAuthenticationContextAccessor context)
        : DeleteGlossaryTermVersionEndpointBase(terms, versions, ancestry, authority, context);

    // Everything an endpoint over versions is built from, with what each part answers and what was saved, deleted or asked.
    private sealed class Rig
    {
        public Mock<IGlossaryTermConfigurationProvider> Terms { get; } = new();

        public Mock<IGlossaryTermVersionConfigurationProvider> Versions { get; } = new();

        public Mock<IPlaceAncestry> Ancestry { get; } = new();

        public Mock<IScopeAuthority> Authority { get; } = new();

        public Mock<IAuthenticationContextAccessor> Context { get; } = new();

        public Mock<IPlaceNames> Names { get; } = new();

        public List<ThingScope> Asked { get; } = [];

        public IGlossaryTermVersionImplementationConfiguration? Saved { get; set; }

        public List<string> Deleted { get; } = [];
    }

    private static IGlossaryTermImplementationConfiguration Term()
    {
        var term = new Mock<IGlossaryTermImplementationConfiguration>();
        term.SetupGet(t => t.Id).Returns(TermId);
        term.SetupGet(t => t.Name).Returns("Churn");
        term.SetupGet(t => t.Definition).Returns("Base definition");
        term.SetupGet(t => t.Owner).Returns("Base owner");
        return term.Object;
    }

    private static async IAsyncEnumerable<T> Stream<T>(IEnumerable<T> rows)
    {
        foreach (var row in rows)
        {
            yield return row;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static GlossaryTermVersionDomainConfiguration Version(string kind, Guid place, string definition) => new()
    {
        Id = Guid.NewGuid(),
        Name = $"Churn@{kind}:{place:D}",
        BaseTermId = TermId,
        TermName = "Churn",
        ScopeKind = kind,
        ScopeId = place,
        Definition = definition,
        Owner = definition + " owner",
    };

    private static IReadOnlyList<PlaceStep> InOrganization() =>
        [new(PlaceKinds.Organization, Finance, 0), new(PlaceKinds.Tenant, Tenant, 1)];

    private static IReadOnlyList<PlaceStep> InWorkspace() =>
        [new(PlaceKinds.Workspace, Workspace, 0), new(PlaceKinds.Organization, Finance, 1), new(PlaceKinds.Tenant, Tenant, 2)];

    private static Rig Create(
        bool termExists = true,
        IReadOnlyList<GlossaryTermVersionDomainConfiguration>? versions = null,
        Func<ThingScope, bool>? may = null,
        bool signedIn = true,
        Guid? organization = null)
    {
        var setup = new Rig();
        var held = versions ?? [];
        var allowed = may ?? (_ => true);

        setup.Terms.Setup(t => t.Get(TermId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(termExists
                ? GenericResult<IGlossaryTermImplementationConfiguration>.Success(Term())
                : GenericResult<IGlossaryTermImplementationConfiguration>.Success(default!));

        setup.Versions.Setup(v => v.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainQuery query, CancellationToken _) => GenericResult<ConfigurationPage<GlossaryTermVersionDomainConfiguration>>.Success(
                new ConfigurationPage<GlossaryTermVersionDomainConfiguration>(query.Skip, query.Take, false, _ => Stream(held))));
        setup.Versions.Setup(v => v.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, CancellationToken _) => GenericResult<IGlossaryTermVersionImplementationConfiguration>.Success(
                Existing(held.FirstOrDefault(row => row.Name == name))!));
        setup.Versions.Setup(v => v.Save(It.IsAny<IGlossaryTermVersionImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((IGlossaryTermVersionImplementationConfiguration saved, string _, string _, string _, CancellationToken _) => setup.Saved = saved)
            .ReturnsAsync(GenericResult.Success());
        setup.Versions.Setup(v => v.Delete(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string name, CancellationToken _) => setup.Deleted.Add(name))
            .ReturnsAsync(GenericResult.Success());

        setup.Ancestry.Setup(a => a.Get(PlaceKinds.Organization, Finance, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IReadOnlyList<PlaceStep>>.Success(InOrganization()));
        setup.Ancestry.Setup(a => a.Get(PlaceKinds.Workspace, Workspace, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IReadOnlyList<PlaceStep>>.Success(InWorkspace()));
        setup.Ancestry.Setup(a => a.Get(PlaceKinds.Tenant, Tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<PlaceStep>>.Success([new PlaceStep(PlaceKinds.Tenant, Tenant, 0)]));
        setup.Ancestry.Setup(a => a.Get(PlaceKinds.Organization, Legal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<PlaceStep>>.Success([new PlaceStep(PlaceKinds.Organization, Legal, 0), new PlaceStep(PlaceKinds.Tenant, OtherTenant, 1)]));

        setup.Authority.Setup(a => a.May(It.IsAny<string>(), It.IsAny<ThingScope>(), It.IsAny<CancellationToken>()))
            .Callback((string _, ThingScope scope, CancellationToken _) => setup.Asked.Add(scope))
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

    private static IGlossaryTermVersionImplementationConfiguration? Existing(GlossaryTermVersionDomainConfiguration? row)
        => row is null
            ? null
            : new GlossaryTermVersionImplementationConfiguration
            {
                Id = row.Id, Name = row.Name, BaseTermId = row.BaseTermId, TermName = row.TermName, ScopeKind = row.ScopeKind, ScopeId = row.ScopeId,
                Definition = row.Definition, Owner = row.Owner,
            };

    private static SaveGlossaryTermVersionRequest Request(string kind = "Organization", Guid? place = null, string definition = "Finance definition", string owner = "Finance owner") => new()
    {
        Id = TermId, PlaceKind = kind, PlaceId = place ?? Finance, Definition = definition, Owner = owner, Formula = "a / b", Steward = "Ana",
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AVersionAtTheCallersOrganizationAppliesAndTheTermItselfIsTheTenantsVersionBehindIt()
    {
        var setup = Create(versions: [Version("Organization", Finance, "Finance definition")], organization: Finance);
        var endpoint = Factory.Create<ListVersions>(setup.Terms.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object, setup.Names.Object);

        await endpoint.HandleAsync(new GlossaryTermVersionsRequest { Id = TermId }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Name.ShouldBe("Churn");
        endpoint.Response.Versions.Select(v => (v.PlaceKind, v.Depth, v.IsBase, v.Applies, v.Definition)).ToArray().ShouldBe(new[]
        {
            ("Organization", 0, false, true, "Finance definition"),
            ("Tenant", 1, true, false, "Base definition"),
        });
        // The same name for each; the place's name follows it in parentheses, except for the tenant's own.
        endpoint.Response.Versions.Select(v => v.Label).ToArray().ShouldBe(new[] { "Churn (Finance)", "Churn" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task WithNoVersionAtTheCallersPlaceTheTermItselfApplies()
    {
        var setup = Create(organization: Finance);
        var endpoint = Factory.Create<ListVersions>(setup.Terms.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object, setup.Names.Object);

        await endpoint.HandleAsync(new GlossaryTermVersionsRequest { Id = TermId }, CancellationToken.None);

        var only = endpoint.Response.Versions.ShouldHaveSingleItem();
        only.IsBase.ShouldBeTrue();
        only.Applies.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AVersionHeldAtAnotherOrganizationIsNotListed()
    {
        var setup = Create(versions: [Version("Organization", Legal, "Legal definition")], organization: Finance);
        var endpoint = Factory.Create<ListVersions>(setup.Terms.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object, setup.Names.Object);

        await endpoint.HandleAsync(new GlossaryTermVersionsRequest { Id = TermId }, CancellationToken.None);

        endpoint.Response.Versions.ShouldHaveSingleItem().IsBase.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AVersionAtTheWorkspaceAppliesFirstForACallerWhoAsksFromThatWorkspace()
    {
        var setup = Create(
            versions: [Version("Workspace", Workspace, "Workspace definition"), Version("Organization", Finance, "Finance definition")],
            organization: Finance);
        var endpoint = Factory.Create<ListVersions>(setup.Terms.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object, setup.Names.Object);

        await endpoint.HandleAsync(new GlossaryTermVersionsRequest { Id = TermId, WorkspaceId = Workspace }, CancellationToken.None);

        endpoint.Response.PlaceKind.ShouldBe("Workspace");
        endpoint.Response.Versions.Select(v => (v.PlaceKind, v.Depth, v.Applies)).ToArray().ShouldBe(new[]
        {
            ("Workspace", 0, true),
            ("Organization", 1, false),
            ("Tenant", 2, false),
        });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task APlaceTheCallerMayNotReadIsAbsentAndTheNextOneApplies()
    {
        var setup = Create(
            versions: [Version("Workspace", Workspace, "Workspace definition"), Version("Organization", Finance, "Finance definition")],
            may: scope => scope.OrganizationId is null);
        var endpoint = Factory.Create<ListVersions>(setup.Terms.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object, setup.Names.Object);

        await endpoint.HandleAsync(new GlossaryTermVersionsRequest { Id = TermId, WorkspaceId = Workspace }, CancellationToken.None);

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
        var endpoint = Factory.Create<ListVersions>(setup.Terms.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object, setup.Names.Object);

        await endpoint.HandleAsync(new GlossaryTermVersionsRequest { Id = TermId, WorkspaceId = Workspace }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ATermThatDoesNotExistIsAnswered404()
    {
        var setup = Create(termExists: false, organization: Finance);
        var endpoint = Factory.Create<ListVersions>(setup.Terms.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object, setup.Names.Object);

        await endpoint.HandleAsync(new GlossaryTermVersionsRequest { Id = TermId }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ACallerWhoIsNotSignedInListsNothing()
    {
        var setup = Create(signedIn: false);
        var endpoint = Factory.Create<ListVersions>(setup.Terms.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object, setup.Names.Object);

        await endpoint.HandleAsync(new GlossaryTermVersionsRequest { Id = TermId }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(401);
        setup.Ancestry.Verify(a => a.Get(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ANewVersionIsSavedUnderTheTermsNameJoinedToThePlaceAndPointsAtTheTerm()
    {
        var setup = Create();
        var endpoint = Factory.Create<SaveVersion>(setup.Terms.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object);

        await endpoint.HandleAsync(Request(), CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        var saved = setup.Saved.ShouldNotBeNull();
        saved.Name.ShouldBe($"Churn@Organization:{Finance:D}");
        saved.BaseTermId.ShouldBe(TermId);
        saved.ScopeKind.ShouldBe("Organization");
        saved.ScopeId.ShouldBe(Finance);
        saved.Definition.ShouldBe("Finance definition");
        saved.Owner.ShouldBe("Finance owner");
        saved.Formula.ShouldBe("a / b");
        setup.Asked.ShouldContain(new ThingScope(Tenant, Finance));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AVersionAlreadyHeldAtThePlaceIsReplacedAndKeepsItsIdentity()
    {
        var held = Version("Organization", Finance, "Old definition");
        var setup = Create(versions: [held]);
        var endpoint = Factory.Create<SaveVersion>(setup.Terms.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object);

        await endpoint.HandleAsync(Request(definition: "New definition"), CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        var saved = setup.Saved.ShouldNotBeNull();
        saved.Id.ShouldBe(held.Id);
        saved.Definition.ShouldBe("New definition");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AVersionCannotBeHeldAtTheTenantBecauseTheTermItselfIsTheTenantsVersion()
    {
        var setup = Create();
        var endpoint = Factory.Create<SaveVersion>(setup.Terms.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object);

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
        var endpoint = Factory.Create<SaveVersion>(setup.Terms.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object);

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
        var endpoint = Factory.Create<SaveVersion>(setup.Terms.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object);

        await endpoint.HandleAsync(Request(), CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(400);
        setup.Saved.ShouldBeNull();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    [InlineData("", "Finance owner")]
    [InlineData("   ", "Finance owner")]
    [InlineData("A definition", "")]
    public async Task AVersionWithNoDefinitionOrNoOwnerIsRefusedAs400(string definition, string owner)
    {
        var setup = Create();
        var endpoint = Factory.Create<SaveVersion>(setup.Terms.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object);

        await endpoint.HandleAsync(Request(definition: definition, owner: owner), CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(400);
        setup.Saved.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AVersionIsDeletedByTheNameItIsHeldUnderAndTheTermIsNotTouched()
    {
        var setup = Create(versions: [Version("Organization", Finance, "Finance definition")]);
        var endpoint = Factory.Create<DeleteVersion>(setup.Terms.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object);

        await endpoint.HandleAsync(new GlossaryTermVersionPlaceRequest { Id = TermId, PlaceKind = "Organization", PlaceId = Finance }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        setup.Deleted.ShouldBe([$"Churn@Organization:{Finance:D}"]);
        setup.Terms.Verify(t => t.Delete(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task DeletingAVersionThatIsNotHeldIsAnswered404()
    {
        var setup = Create();
        var endpoint = Factory.Create<DeleteVersion>(setup.Terms.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object);

        await endpoint.HandleAsync(new GlossaryTermVersionPlaceRequest { Id = TermId, PlaceKind = "Organization", PlaceId = Finance }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
        setup.Deleted.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ACallerWithoutTheWriteRightDeletesNothing()
    {
        var setup = Create(versions: [Version("Organization", Finance, "Finance definition")], may: _ => false);
        var endpoint = Factory.Create<DeleteVersion>(setup.Terms.Object, setup.Versions.Object, setup.Ancestry.Object, setup.Authority.Object, setup.Context.Object);

        await endpoint.HandleAsync(new GlossaryTermVersionPlaceRequest { Id = TermId, PlaceKind = "Organization", PlaceId = Finance }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(400);
        setup.Deleted.ShouldBeEmpty();
    }
}
