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
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");

    private sealed class SetTenant(IFeatureEnablement features, IAdministrativeAudit audit)
        : SetTenantFeatureEndpointBase(NullLogger.Instance, features, audit)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class SetOrganization(IFeatureEnablement features, IAdministrativeAudit audit)
        : SetOrganizationFeatureEndpointBase(NullLogger.Instance, features, audit)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class ListTenant(IFeatureEnablement features)
        : ListTenantFeaturesEndpointBase(NullLogger.Instance, features)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class ListCatalogue() : ListFeatureCatalogueEndpointBase(NullLogger.Instance)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private static Action<DefaultHttpContext> ActingIn(Guid tenantId)
        => ctx => ctx.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimDefinitions.tenantId.Name, tenantId.ToString())], "test"));

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
        features.Setup(f => f.SetForOrganization(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(result);
        features.Setup(f => f.ForTenant(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<FeatureState>>.Success([on]));
        return features;
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASwitchOfAFeatureForATenantIsRecordedAtThePlatformLevelNamingTheTenant()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var endpoint = Factory.Create<SetTenant>(ActingIn(Tenant), Features().Object, Audit(recorded).Object);

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
    public async Task ASwitchTheServiceRefusesIsAnsweredFromItsFailureAndRecordsNothing()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var endpoint = Factory.Create<SetTenant>(ActingIn(Tenant), Features(fails: true).Object, Audit(recorded).Object);

        await endpoint.HandleAsync(new SetTenantFeatureRequest { TenantId = Tenant, Feature = "Documents", Enabled = true }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(400);
        recorded.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASwitchMadeButNotRecordedIsAnswered500()
    {
        var endpoint = Factory.Create<SetTenant>(ActingIn(Tenant), Features().Object, Audit(succeeds: false).Object);

        await endpoint.HandleAsync(new SetTenantFeatureRequest { TenantId = Tenant, Feature = "Documents", Enabled = true }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(500);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ATenantSwitchesAFeatureOnForItsOrganizationAndItIsRecordedAtTheOrganizationLevelNamingTheOrganization()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var endpoint = Factory.Create<SetOrganization>(ActingIn(Tenant), Features().Object, Audit(recorded).Object);

        await endpoint.HandleAsync(new SetOrganizationFeatureRequest { OrganizationId = Finance, Feature = "Documents", Enabled = true }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        var entry = recorded.ShouldHaveSingleItem();
        entry.Level.Name.ShouldBe("Organization");
        entry.OrganizationId.ShouldBe(Finance);
        entry.ItemId.ShouldBe($"{Finance}:Documents");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ATenantsFeaturesAreListedFromTheService()
    {
        var features = Features();
        var endpoint = Factory.Create<ListTenant>(ActingIn(Tenant), features.Object);

        await endpoint.HandleAsync(new TenantFeaturesRequest { TenantId = Tenant }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Features.ShouldHaveSingleItem().Name.ShouldBe("Documents");
        features.Verify(f => f.ForTenant(Tenant, It.IsAny<CancellationToken>()), Times.Once);
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
