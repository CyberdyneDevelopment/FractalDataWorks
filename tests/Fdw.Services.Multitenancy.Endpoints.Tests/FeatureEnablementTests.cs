using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Multitenancy.Endpoints.Tests;

/// <summary>
/// Tests for the feature switches over tenants and organizations (FDW-1580): a feature is on for a tenant when the platform switched it
/// on, on for an organization when its tenant did too, and off at a higher level means off below.
/// </summary>
public sealed class FeatureEnablementTests
{
    private static readonly Guid Tenant = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");
    private static readonly FeatureTables Tables = new("PlatformConfiguration", "tenant", "TenantFeatures", "OrganizationFeatures");

    private static (FeatureEnablement Service, Mock<IConfigurationGateway> Gateway) Create(
        string[] tenantFeatures, string[] organizationFeatures, bool gatewayFails = false)
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway.Setup(g => g.Execute<IEnumerable<TenantFeatureRow>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<TenantFeatureRow>>.Success(
                [.. tenantFeatures.Select(feature => new TenantFeatureRow { Id = Guid.NewGuid(), TenantId = Tenant, Feature = feature })]));
        gateway.Setup(g => g.Execute<IEnumerable<OrganizationFeatureRow>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<OrganizationFeatureRow>>.Success(
                [.. organizationFeatures.Select(feature => new OrganizationFeatureRow { Id = Guid.NewGuid(), TenantId = Tenant, OrganizationId = Finance, Feature = feature })]));
        gateway.Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Success(1));
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(g => g.Get("PlatformConfiguration")).Returns(gatewayFails
            ? GenericResult<IConfigurationGateway>.Failure(new GenericMessage("the configuration gateway is down"))
            : GenericResult<IConfigurationGateway>.Success(gateway.Object));
        return (new FeatureEnablement(gateways.Object, Tables, RetentionTestScope.Organizations().Object, RetentionTestScope.Authority(true).Object, NullLogger<FeatureEnablement>.Instance), gateway);
    }

    private static void Writes(Mock<IConfigurationGateway> gateway, Times times)
        => gateway.Verify(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), times);

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ATenantHasWhatThePlatformSwitchedOnAndTheRestIsOffAtThePlatform()
    {
        var (service, _) = Create(["Documents"], []);

        var states = await service.ForTenant(Tenant);

        states.IsSuccess.ShouldBeTrue();
        var documents = states.Value!.Single(state => state.Name == "Documents");
        documents.Enabled.ShouldBeTrue();
        documents.OffAt.ShouldBeNull();
        var studio = states.Value!.Single(state => state.Name == "InsightStudio");
        studio.Enabled.ShouldBeFalse();
        studio.OffAt.ShouldBe(FeatureState.PlatformLevel);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFeatureOffForTheTenantIsOffForItsOrganizationsAtThePlatformEvenWithAnOrganizationRow()
    {
        var (service, _) = Create([], ["Documents"]);

        var states = await service.ForOrganization(Finance);

        var documents = states.Value!.Single(state => state.Name == "Documents");
        documents.Enabled.ShouldBeFalse();
        documents.OffAt.ShouldBe(FeatureState.PlatformLevel);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFeatureTheTenantHasButDidNotSwitchOnForTheOrganizationIsOffAtTheTenant()
    {
        var (service, _) = Create(["Documents"], []);

        var states = await service.ForOrganization(Finance);

        var documents = states.Value!.Single(state => state.Name == "Documents");
        documents.Enabled.ShouldBeFalse();
        documents.OffAt.ShouldBe(FeatureState.TenantLevel);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFeatureSwitchedOnAtBothLevelsIsOnForTheOrganization()
    {
        var (service, _) = Create(["Documents"], ["Documents"]);

        var states = await service.ForOrganization(Finance);

        states.Value!.Single(state => state.Name == "Documents").Enabled.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ATenantCannotSwitchOnForAnOrganizationWhatThePlatformHasNotSwitchedOnForTheTenant()
    {
        var (service, gateway) = Create([], []);

        var set = await service.SetForOrganization(Finance, "Documents", true);

        set.IsFailure.ShouldBeTrue();
        Writes(gateway, Times.Never());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ATenantSwitchesOnForAnOrganizationWhatThePlatformSwitchedOnForTheTenant()
    {
        var (service, gateway) = Create(["Documents"], []);

        var set = await service.SetForOrganization(Finance, "Documents", true);

        set.IsSuccess.ShouldBeTrue();
        Writes(gateway, Times.Once());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task SwitchingOffForAnOrganizationRemovesItsRowEvenWhenTheTenantLostTheFeature()
    {
        var (service, gateway) = Create([], ["Documents"]);

        var set = await service.SetForOrganization(Finance, "Documents", false);

        set.IsSuccess.ShouldBeTrue();
        Writes(gateway, Times.Once());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task SwitchingToTheStateAFeatureIsAlreadyInWritesNothing()
    {
        var (service, gateway) = Create(["Documents"], []);

        var on = await service.SetForTenant(Tenant, "Documents", true);
        var off = await service.SetForTenant(Tenant, "InsightStudio", false);

        on.IsSuccess.ShouldBeTrue();
        off.IsSuccess.ShouldBeTrue();
        Writes(gateway, Times.Never());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ThePlatformSwitchesAFeatureOnAndOffForATenantWithOneWriteEach()
    {
        var (on, onGateway) = Create([], []);
        var (off, offGateway) = Create(["Documents"], []);

        (await on.SetForTenant(Tenant, "Documents", true)).IsSuccess.ShouldBeTrue();
        (await off.SetForTenant(Tenant, "Documents", false)).IsSuccess.ShouldBeTrue();

        Writes(onGateway, Times.Once());
        Writes(offGateway, Times.Once());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFeatureTheCatalogueDoesNotHoldIsNotFoundAndNothingIsRead()
    {
        var (service, gateway) = Create([], []);

        var set = await service.SetForTenant(Tenant, "Nothing", true);

        set.IsFailure.ShouldBeTrue();
        Writes(gateway, Times.Never());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AGatewayThatIsDownFailsTheReadInsteadOfAnsweringOff()
    {
        var (service, _) = Create([], [], gatewayFails: true);

        var states = await service.ForTenant(Tenant);

        states.IsFailure.ShouldBeTrue();
    }
}
