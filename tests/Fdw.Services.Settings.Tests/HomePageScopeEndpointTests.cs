using System;
using System.Collections.Generic;
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
/// Tests for the tenant and person home page routes and the audit of every shared page's changes (FDW-2471): a tenant's page needs the right at the
/// tenant's scope, a person reaches only their own page without a right, and a save or publish of a tenant's or an organization's page is recorded
/// while a person's own is not.
/// </summary>
[Trait("Category", "Security")]
public sealed class HomePageScopeEndpointTests
{
    private const string Page = """{"version":1,"cards":[]}""";

    private static readonly Guid Tenant = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");
    private static readonly Guid Person = new("55555555-0000-0000-0000-000000000001");

    private sealed class GetTenant(IHomePages pages, IScopeAuthority authority) : GetTenantHomePageEndpointBase(pages, authority)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class SaveTenant(IHomePages pages, IScopeAuthority authority, IAdministrativeAudit audit)
        : SaveTenantHomePageDraftEndpointBase(pages, authority, audit, NullLogger.Instance)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class PublishTenant(IHomePages pages, IScopeAuthority authority, IAdministrativeAudit audit)
        : PublishTenantHomePageEndpointBase(pages, authority, audit, NullLogger.Instance)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class GetPerson(IHomePages pages, IAuthenticationContextAccessor authContext) : GetPersonHomePageEndpointBase(pages, authContext)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class SavePerson(IHomePages pages, IAuthenticationContextAccessor authContext, IAdministrativeAudit audit)
        : SavePersonHomePageDraftEndpointBase(pages, authContext, audit, NullLogger.Instance)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class PublishPerson(IHomePages pages, IAuthenticationContextAccessor authContext, IAdministrativeAudit audit)
        : PublishPersonHomePageEndpointBase(pages, authContext, audit, NullLogger.Instance)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class SaveOrganization(IHomePages pages, IOrganizationScope organizations, IScopeAuthority authority, IAdministrativeAudit audit)
        : SaveOrganizationHomePageDraftEndpointBase(pages, organizations, authority, audit, NullLogger.Instance)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private static Mock<IScopeAuthority> Authority(bool may)
    {
        var authority = new Mock<IScopeAuthority>();
        authority.Setup(a => a.May(It.IsAny<string>(), It.IsAny<ThingScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(may ? GenericResult.Success() : GenericResult.Failure(new GenericMessage("the caller does not hold the right")));
        return authority;
    }

    private static Mock<IAdministrativeAudit> Audit(List<AdministrativeChangeEntry>? recorded = null, bool succeeds = true)
    {
        var audit = new Mock<IAdministrativeAudit>();
        audit.Setup(a => a.Record(It.IsAny<AdministrativeChangeEntry>(), It.IsAny<CancellationToken>()))
            .Callback((AdministrativeChangeEntry entry, CancellationToken _) => recorded?.Add(entry))
            .ReturnsAsync(succeeds ? GenericResult.Success() : GenericResult.Failure(new GenericMessage("the audit store is down")));
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
        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupGet(a => a.Current).Returns(context.Object);
        return accessor;
    }

    private static Mock<IOrganizationScope> Organizations()
    {
        var organizations = new Mock<IOrganizationScope>();
        organizations.Setup(o => o.TenantOf(Finance, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<Guid>.Success(Tenant));
        return organizations;
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ATenantsPageIsReadWhenTheCallerHoldsTheReadRightAtTheTenantsScope()
    {
        var authority = Authority(may: true);
        var endpoint = Factory.Create<GetTenant>(Pages().Object, authority.Object);

        await endpoint.HandleAsync(new TenantHomePageRequest { TenantId = Tenant }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Published.ShouldNotBeNull();
        authority.Verify(a => a.May("home-page:read", new ThingScope(Tenant, null), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ACallerWithoutTheReadRightReadsNoTenantPage()
    {
        var pages = Pages();
        var endpoint = Factory.Create<GetTenant>(pages.Object, Authority(may: false).Object);

        await endpoint.HandleAsync(new TenantHomePageRequest { TenantId = Tenant }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(400);
        pages.Verify(p => p.Get(It.IsAny<HomePageScope>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ATenantDraftIsSavedAtTheTenantAndRecordedAtTheTenantLevel()
    {
        var pages = Pages();
        var recorded = new List<AdministrativeChangeEntry>();
        var authority = Authority(may: true);
        var endpoint = Factory.Create<SaveTenant>(pages.Object, authority.Object, Audit(recorded).Object);
        using var document = JsonDocument.Parse(Page);

        await endpoint.HandleAsync(new SaveTenantHomePageDraftRequest { TenantId = Tenant, Page = document.RootElement }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        pages.Verify(p => p.SaveDraft(new HomePageScope(HomePageKinds.Tenant, Tenant, Tenant), Page, It.IsAny<CancellationToken>()), Times.Once);
        authority.Verify(a => a.May("home-page:write", new ThingScope(Tenant, null), It.IsAny<CancellationToken>()), Times.Once);
        var entry = recorded.ShouldHaveSingleItem();
        entry.ChangeKind.ShouldBe("HomePageDraftSaved");
        entry.ItemKind.ShouldBe("HomePage");
        entry.ItemId.ShouldBe($"Tenant:{Tenant:D}");
        entry.Level.Name.ShouldBe("Tenant");
        entry.OrganizationId.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ACallerWithoutTheWriteRightSavesAndRecordsNothingForATenant()
    {
        var pages = Pages();
        var recorded = new List<AdministrativeChangeEntry>();
        var endpoint = Factory.Create<SaveTenant>(pages.Object, Authority(may: false).Object, Audit(recorded).Object);
        using var document = JsonDocument.Parse(Page);

        await endpoint.HandleAsync(new SaveTenantHomePageDraftRequest { TenantId = Tenant, Page = document.RootElement }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(400);
        pages.Verify(p => p.SaveDraft(It.IsAny<HomePageScope>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        recorded.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ATenantPublishIsRecordedAtTheTenantLevel()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var endpoint = Factory.Create<PublishTenant>(Pages().Object, Authority(may: true).Object, Audit(recorded).Object);

        await endpoint.HandleAsync(new TenantHomePageRequest { TenantId = Tenant }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        var entry = recorded.ShouldHaveSingleItem();
        entry.ChangeKind.ShouldBe("HomePagePublished");
        entry.Level.Name.ShouldBe("Tenant");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AChangeThatCannotBeRecordedIsAnsweredAsAFailureNotASuccess()
    {
        var endpoint = Factory.Create<PublishTenant>(Pages().Object, Authority(may: true).Object, Audit(succeeds: false).Object);

        await endpoint.HandleAsync(new TenantHomePageRequest { TenantId = Tenant }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(400);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnOrganizationDraftIsRecordedAtTheOrganizationLevelNamingTheOrganization()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var endpoint = Factory.Create<SaveOrganization>(Pages().Object, Organizations().Object, Authority(may: true).Object, Audit(recorded).Object);
        using var document = JsonDocument.Parse(Page);

        await endpoint.HandleAsync(new SaveOrganizationHomePageDraftRequest { OrganizationId = Finance, Page = document.RootElement }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        var entry = recorded.ShouldHaveSingleItem();
        entry.ChangeKind.ShouldBe("HomePageDraftSaved");
        entry.ItemId.ShouldBe($"Organization:{Finance:D}");
        entry.Level.Name.ShouldBe("Organization");
        entry.OrganizationId.ShouldBe(Finance);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task APersonReadsTheirOwnPageWithoutAnyRight()
    {
        var pages = Pages();
        var endpoint = Factory.Create<GetPerson>(pages.Object, Caller(signedIn: true).Object);

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        pages.Verify(p => p.Get(new HomePageScope(HomePageKinds.Person, Person, Tenant), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task APersonWhoIsNotSignedInReachesNoPersonPage()
    {
        var pages = Pages();
        var endpoint = Factory.Create<GetPerson>(pages.Object, Caller(signedIn: false).Object);

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(401);
        pages.Verify(p => p.Get(It.IsAny<HomePageScope>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task APersonsOwnDraftIsSavedAtThemselvesAndNotRecordedInTheAdministrativeAudit()
    {
        var pages = Pages();
        var audit = Audit();
        var endpoint = Factory.Create<SavePerson>(pages.Object, Caller(signedIn: true).Object, audit.Object);
        using var document = JsonDocument.Parse(Page);

        await endpoint.HandleAsync(new SavePersonHomePageDraftRequest { Page = document.RootElement }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        pages.Verify(p => p.SaveDraft(new HomePageScope(HomePageKinds.Person, Person, Tenant), Page, It.IsAny<CancellationToken>()), Times.Once);
        audit.Verify(a => a.Record(It.IsAny<AdministrativeChangeEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task APersonsOwnPublishIsMadeAtThemselves()
    {
        var pages = Pages();
        var endpoint = Factory.Create<PublishPerson>(pages.Object, Caller(signedIn: true).Object, Audit().Object);

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        pages.Verify(p => p.Publish(new HomePageScope(HomePageKinds.Person, Person, Tenant), It.IsAny<CancellationToken>()), Times.Once);
    }
}
