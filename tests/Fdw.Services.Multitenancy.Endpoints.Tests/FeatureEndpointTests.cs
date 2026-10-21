using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Claims.Abstractions;
using Fdw.Messages;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Multitenancy.Endpoints.Tests;

/// <summary>
/// Tests for the endpoints over the feature switches (FDW-1580): the platform switches a feature for a tenant, the tenant for its
/// organizations, each switch is recorded in the audit, and a switch that cannot be recorded answers 500.
/// </summary>
[Trait("Category", "Security")]
public sealed class FeatureEndpointTests
{
    private static readonly Guid Tenant = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid OtherTenant = new("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");

    private sealed class SetTenant(IFeatureEnablement features, IAdministrativeAudit audit, ISystemRoleConfiguration roles)
        : SetTenantFeatureEndpointBase(NullLogger.Instance, features, audit, roles)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class SetOrganization(IFeatureEnablement features, IOrganizationProvider organizations, IAdministrativeAudit audit, ISystemRoleConfiguration roles)
        : SetOrganizationFeatureEndpointBase(NullLogger.Instance, features, organizations, audit, roles)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class ListTenant(IFeatureEnablement features, ISystemRoleConfiguration roles)
        : ListTenantFeaturesEndpointBase(NullLogger.Instance, features, roles)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class ListCatalogue() : ListFeatureCatalogueEndpointBase(NullLogger.Instance)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private static Action<DefaultHttpContext> ActingIn(Guid tenantId)
        => ctx => ctx.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimDefinitions.tenantId.Name, tenantId.ToString())], "test"));

    private static ISystemRoleConfiguration Roles(bool platformAdmin)
    {
        var roles = new Mock<ISystemRoleConfiguration>();
        roles.Setup(r => r.IsPlatformAdmin(It.IsAny<ClaimsPrincipal>())).Returns(platformAdmin);
        return roles.Object;
    }

    private static Mock<IAdministrativeAudit> Audit(List<AdministrativeChangeEntry>? recorded = null, bool succeeds = true)
    {
        var audit = new Mock<IAdministrativeAudit>();
        audit.Setup(a => a.Record(It.IsAny<AdministrativeChangeEntry>(), It.IsAny<CancellationToken>()))
            .Callback((AdministrativeChangeEntry entry, CancellationToken _) => recorded?.Add(entry))
            .ReturnsAsync(succeeds ? GenericResult.Success() : GenericResult.Failure(new GenericMessage("the audit store is down")));
        return audit;
    }

    private static Mock<IFeatureEnablement> Features(bool fails = false)
    {
        var features = new Mock<IFeatureEnablement>();
        var on = new FeatureState("Documents", "Documents", "Upload documents.", true, null);
        var result = fails
            ? GenericResult<FeatureState>.Failure(new GenericMessage("the platform has not switched the feature on for the tenant"))
            : GenericResult<FeatureState>.Success(on);
        features.Setup(f => f.SetForTenant(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(result);
        features.Setup(f => f.SetForOrganization(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(result);
        features.Setup(f => f.ForTenant(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<FeatureState>>.Success([on]));
        return features;
    }

    private static Mock<IOrganizationProvider> Organizations()
    {
        var organizations = new Mock<IOrganizationProvider>();
        organizations.Setup(o => o.Get(Finance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Success(new OrganizationConfiguration { TenantId = Tenant }));
        organizations.Setup(o => o.Get(It.Is<Guid>(id => id != Finance), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Failure(new GenericMessage("no such organization")));
        return organizations;
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ThePlatformSwitchesAFeatureOnForATenantAndItIsRecordedAtThePlatformLevel()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var endpoint = Factory.Create<SetTenant>(ActingIn(OtherTenant), Features().Object, Audit(recorded).Object, Roles(true));

        await endpoint.HandleAsync(new SetTenantFeatureRequest { TenantId = Tenant, Feature = "Documents", Enabled = true }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Enabled.ShouldBeTrue();
        var entry = recorded.ShouldHaveSingleItem();
        entry.ChangeKind.ShouldBe("FeatureEnabled");
        entry.ItemId.ShouldBe($"{Tenant}:Documents");
        entry.Level.Name.ShouldBe("Platform");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OnlyAPlatformAdministratorSwitchesAFeatureForATenant()
    {
        var features = Features();
        var endpoint = Factory.Create<SetTenant>(ActingIn(Tenant), features.Object, Audit().Object, Roles(false));

        await endpoint.HandleAsync(new SetTenantFeatureRequest { TenantId = Tenant, Feature = "Documents", Enabled = true }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(403);
        features.Verify(f => f.SetForTenant(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASwitchTheServiceRefusesIsAnsweredFromItsFailureAndRecordsNothing()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var endpoint = Factory.Create<SetTenant>(ActingIn(Tenant), Features(fails: true).Object, Audit(recorded).Object, Roles(true));

        await endpoint.HandleAsync(new SetTenantFeatureRequest { TenantId = Tenant, Feature = "Documents", Enabled = true }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(400);
        recorded.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASwitchMadeButNotRecordedIsAnswered500()
    {
        var endpoint = Factory.Create<SetTenant>(ActingIn(Tenant), Features().Object, Audit(succeeds: false).Object, Roles(true));

        await endpoint.HandleAsync(new SetTenantFeatureRequest { TenantId = Tenant, Feature = "Documents", Enabled = true }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(500);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ATenantSwitchesAFeatureOnForItsOrganizationAndItIsRecordedAtTheOrganizationLevelNamingTheOrganization()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var endpoint = Factory.Create<SetOrganization>(ActingIn(Tenant), Features().Object, Organizations().Object, Audit(recorded).Object, Roles(false));

        await endpoint.HandleAsync(new SetOrganizationFeatureRequest { OrganizationId = Finance, Feature = "Documents", Enabled = true }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        var entry = recorded.ShouldHaveSingleItem();
        entry.Level.Name.ShouldBe("Organization");
        entry.OrganizationId.ShouldBe(Finance);
        entry.ItemId.ShouldBe($"{Finance}:Documents");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnotherTenantsCallerCannotSwitchAFeatureForTheOrganization()
    {
        var features = Features();
        var endpoint = Factory.Create<SetOrganization>(ActingIn(OtherTenant), features.Object, Organizations().Object, Audit().Object, Roles(false));

        await endpoint.HandleAsync(new SetOrganizationFeatureRequest { OrganizationId = Finance, Feature = "Documents", Enabled = true }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(403);
        features.Verify(f => f.SetForOrganization(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnOrganizationThatDoesNotExistIsRefusedBeforeAnythingIsSwitched()
    {
        var features = Features();
        var endpoint = Factory.Create<SetOrganization>(ActingIn(Tenant), features.Object, Organizations().Object, Audit().Object, Roles(true));

        await endpoint.HandleAsync(new SetOrganizationFeatureRequest { OrganizationId = Guid.NewGuid(), Feature = "Documents", Enabled = true }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(400);
        features.Verify(f => f.SetForOrganization(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ATenantReadsItsOwnFeaturesButNotAnotherTenants()
    {
        var features = Features();
        var own = Factory.Create<ListTenant>(ActingIn(Tenant), features.Object, Roles(false));
        var other = Factory.Create<ListTenant>(ActingIn(OtherTenant), features.Object, Roles(false));

        await own.HandleAsync(new TenantFeaturesRequest { TenantId = Tenant }, CancellationToken.None);
        await other.HandleAsync(new TenantFeaturesRequest { TenantId = Tenant }, CancellationToken.None);

        own.HttpContext.Response.StatusCode.ShouldBe(200);
        own.Response.Features.ShouldHaveSingleItem().Name.ShouldBe("Documents");
        other.HttpContext.Response.StatusCode.ShouldBe(403);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task TheCatalogueListsDocumentsAndInsightStudio()
    {
        var endpoint = Factory.Create<ListCatalogue>(ActingIn(Tenant));

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Features.ShouldContain(feature => feature.Name == "Documents");
        endpoint.Response.Features.ShouldContain(feature => feature.Name == "InsightStudio");
    }
}
