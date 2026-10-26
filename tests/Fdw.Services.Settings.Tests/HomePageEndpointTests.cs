using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Messages;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Settings.Tests;

/// <summary>
/// Tests for the home page endpoints (FDW-2325): an organization's page is reached only by a caller who acts in it and holds the right at
/// its scope, and the page that applies to the caller needs no right but a signed-in person.
/// </summary>
[Trait("Category", "Security")]
public sealed class HomePageEndpointTests
{
    private const string Page = """{"version":1,"cards":[]}""";

    private static readonly Guid Tenant = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");
    private static readonly Guid Person = new("55555555-0000-0000-0000-000000000001");

    private sealed class Get(IHomePages pages, IOrganizationScope organizations, IScopeAuthority authority)
        : GetOrganizationHomePageEndpointBase(pages, organizations, authority)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class Save(IHomePages pages, IOrganizationScope organizations, IScopeAuthority authority, IAdministrativeAudit audit)
        : SaveOrganizationHomePageDraftEndpointBase(pages, organizations, authority, audit, NullLogger.Instance)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class Publish(IHomePages pages, IOrganizationScope organizations, IScopeAuthority authority, IAdministrativeAudit audit)
        : PublishOrganizationHomePageEndpointBase(pages, organizations, authority, audit, NullLogger.Instance)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class Applying(IHomePages pages, IAuthenticationContextAccessor authContext)
        : GetHomePageEndpointBase(pages, authContext)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private static Mock<IOrganizationScope> Organizations(bool actingIn = true)
    {
        var organizations = new Mock<IOrganizationScope>();
        organizations.Setup(o => o.TenantOf(Finance, It.IsAny<CancellationToken>())).ReturnsAsync(actingIn
            ? GenericResult<Guid>.Success(Tenant)
            : GenericResult<Guid>.Failure(new GenericMessage("the caller does not act in the organization")));
        return organizations;
    }

    private static Mock<IScopeAuthority> Authority(bool may)
    {
        var authority = new Mock<IScopeAuthority>();
        authority.Setup(a => a.May(It.IsAny<string>(), It.IsAny<ThingScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(may ? GenericResult.Success() : GenericResult.Failure(new GenericMessage("the caller does not hold the right")));
        return authority;
    }

    private static Mock<IAdministrativeAudit> Audit()
    {
        var audit = new Mock<IAdministrativeAudit>();
        audit.Setup(a => a.Record(It.IsAny<AdministrativeChangeEntry>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        return audit;
    }

    private static Mock<IHomePages> Pages()
    {
        var pages = new Mock<IHomePages>();
        pages.Setup(p => p.Get(It.IsAny<HomePageScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<HomePageSet>.Success(new HomePageSet(Page, null)));
        pages.Setup(p => p.SaveDraft(It.IsAny<HomePageScope>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        pages.Setup(p => p.Publish(It.IsAny<HomePageScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        return pages;
    }

    private static Mock<IAuthenticationContextAccessor> Caller(bool signedIn)
    {
        var context = new Mock<IAuthenticationContext>();
        context.SetupGet(c => c.IsAuthenticated).Returns(signedIn);
        context.SetupGet(c => c.UserId).Returns(Person.ToString());
        context.SetupGet(c => c.ActiveTenantId).Returns(Tenant);
        context.SetupGet(c => c.ActiveOrgId).Returns(Finance);
        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupGet(a => a.Current).Returns(context.Object);
        return accessor;
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnOrganizationsPageIsReadWhenTheCallerActsInItAndHoldsTheReadRightAtItsScope()
    {
        var authority = Authority(may: true);
        var endpoint = Factory.Create<Get>(Pages().Object, Organizations().Object, authority.Object);

        await endpoint.HandleAsync(new OrganizationHomePageRequest { OrganizationId = Finance }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Published.ShouldNotBeNull();
        endpoint.Response.Draft.ShouldBeNull();
        authority.Verify(a => a.May("home-page:read", new ThingScope(Tenant, Finance), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ACallerNotActingInTheOrganizationIsRefusedAndNothingIsRead()
    {
        var pages = Pages();
        var endpoint = Factory.Create<Get>(pages.Object, Organizations(actingIn: false).Object, Authority(may: true).Object);

        await endpoint.HandleAsync(new OrganizationHomePageRequest { OrganizationId = Finance }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(400);
        pages.Verify(p => p.Get(It.IsAny<HomePageScope>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ACallerWithoutTheReadRightIsRefusedAndNothingIsRead()
    {
        var pages = Pages();
        var endpoint = Factory.Create<Get>(pages.Object, Organizations().Object, Authority(may: false).Object);

        await endpoint.HandleAsync(new OrganizationHomePageRequest { OrganizationId = Finance }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(400);
        pages.Verify(p => p.Get(It.IsAny<HomePageScope>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ADraftIsSavedForTheOrganizationWhenTheCallerHoldsTheWriteRight()
    {
        var pages = Pages();
        var authority = Authority(may: true);
        var endpoint = Factory.Create<Save>(pages.Object, Organizations().Object, authority.Object, Audit().Object);
        using var document = JsonDocument.Parse(Page);

        await endpoint.HandleAsync(new SaveOrganizationHomePageDraftRequest { OrganizationId = Finance, Page = document.RootElement }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        pages.Verify(p => p.SaveDraft(new HomePageScope(HomePageKinds.Organization, Finance, Tenant), Page, It.IsAny<CancellationToken>()), Times.Once);
        authority.Verify(a => a.May("home-page:write", new ThingScope(Tenant, Finance), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ACallerWithoutTheWriteRightSavesNothing()
    {
        var pages = Pages();
        var endpoint = Factory.Create<Save>(pages.Object, Organizations().Object, Authority(may: false).Object, Audit().Object);
        using var document = JsonDocument.Parse(Page);

        await endpoint.HandleAsync(new SaveOrganizationHomePageDraftRequest { OrganizationId = Finance, Page = document.RootElement }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(400);
        pages.Verify(p => p.SaveDraft(It.IsAny<HomePageScope>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ADraftTheServiceRefusesIsAnsweredFromItsFailure()
    {
        var pages = Pages();
        pages.Setup(p => p.SaveDraft(It.IsAny<HomePageScope>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new GenericMessage("the page is not a JSON object")));
        var endpoint = Factory.Create<Save>(pages.Object, Organizations().Object, Authority(may: true).Object, Audit().Object);
        using var document = JsonDocument.Parse("[]");

        await endpoint.HandleAsync(new SaveOrganizationHomePageDraftRequest { OrganizationId = Finance, Page = document.RootElement }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(400);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task APublishNeedsTheWriteRightAndPublishesTheOrganizationsDraft()
    {
        var pages = Pages();
        var authority = Authority(may: true);
        var endpoint = Factory.Create<Publish>(pages.Object, Organizations().Object, authority.Object, Audit().Object);

        await endpoint.HandleAsync(new OrganizationHomePageRequest { OrganizationId = Finance }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        pages.Verify(p => p.Publish(new HomePageScope(HomePageKinds.Organization, Finance, Tenant), It.IsAny<CancellationToken>()), Times.Once);
        authority.Verify(a => a.May("home-page:write", new ThingScope(Tenant, Finance), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ACallerWithoutTheWriteRightPublishesNothing()
    {
        var pages = Pages();
        var endpoint = Factory.Create<Publish>(pages.Object, Organizations().Object, Authority(may: false).Object, Audit().Object);

        await endpoint.HandleAsync(new OrganizationHomePageRequest { OrganizationId = Finance }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(400);
        pages.Verify(p => p.Publish(It.IsAny<HomePageScope>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ThePageThatAppliesIsAnsweredWithItsSourceForASignedInCaller()
    {
        var pages = new Mock<IHomePages>();
        pages.Setup(p => p.Applying(Person, Tenant, Finance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<AppliedHomePage>.Success(new AppliedHomePage(HomePageKinds.Organization, Finance, Page)));
        var endpoint = Factory.Create<Applying>(pages.Object, Caller(signedIn: true).Object);

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Source.Kind.ShouldBe(HomePageKinds.Organization);
        endpoint.Response.Source.Name.ShouldBe(Finance.ToString("D"));
        endpoint.Response.Page.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task WhenNoScopeHoldsAPageTheSourceIsNoneAndThePageIsNull()
    {
        var pages = new Mock<IHomePages>();
        pages.Setup(p => p.Applying(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<AppliedHomePage>.Success(new AppliedHomePage(HomePageKinds.None, null, null)));
        var endpoint = Factory.Create<Applying>(pages.Object, Caller(signedIn: true).Object);

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Source.Kind.ShouldBe(HomePageKinds.None);
        endpoint.Response.Source.Name.ShouldBeNull();
        endpoint.Response.Page.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ACallerWhoIsNotSignedInIsAnswered401AndNothingIsRead()
    {
        var pages = new Mock<IHomePages>();
        var endpoint = Factory.Create<Applying>(pages.Object, Caller(signedIn: false).Object);

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(401);
        pages.Verify(p => p.Applying(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
