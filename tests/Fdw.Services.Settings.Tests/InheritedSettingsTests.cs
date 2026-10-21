using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Settings.Tests;

/// <summary>
/// Tests for the settings that inherit down the platform, tenant and organization tree and can be locked from above (FDW-1579): the
/// nearest value wins, a lock held by a level binds the levels below it, a refused write saves nothing, and a lock is lifted.
/// </summary>
[Trait("Category", "Configuration")]
public sealed class InheritedSettingsTests
{
    private static readonly Guid Tenant = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");

    private readonly List<ServerSettingImplementationConfiguration> _server = [];
    private readonly List<TenantSettingImplementationConfiguration> _tenants = [];
    private readonly List<OrganizationSettingImplementationConfiguration> _organizations = [];
    private readonly Mock<IServerSettingConfigurationProvider> _serverProvider = new();
    private readonly Mock<ITenantSettingConfigurationProvider> _tenantProvider = new();
    private readonly Mock<IOrganizationSettingConfigurationProvider> _organizationProvider = new();
    private readonly Mock<IOrganizationScope> _scope = new();

    public InheritedSettingsTests()
    {
        _serverProvider.Setup(p => p.Get(It.IsAny<CancellationToken>())).Returns(() =>
            Task.FromResult<IGenericResult<IReadOnlyList<IServerSettingImplementationConfiguration>>>(
                GenericResult<IReadOnlyList<IServerSettingImplementationConfiguration>>.Success([.. _server])));
        _tenantProvider.Setup(p => p.Get(It.IsAny<CancellationToken>())).Returns(() =>
            Task.FromResult<IGenericResult<IReadOnlyList<ITenantSettingImplementationConfiguration>>>(
                GenericResult<IReadOnlyList<ITenantSettingImplementationConfiguration>>.Success([.. _tenants])));
        _organizationProvider.Setup(p => p.Get(It.IsAny<CancellationToken>())).Returns(() =>
            Task.FromResult<IGenericResult<IReadOnlyList<IOrganizationSettingImplementationConfiguration>>>(
                GenericResult<IReadOnlyList<IOrganizationSettingImplementationConfiguration>>.Success([.. _organizations])));
        _serverProvider.Setup(p => p.Save(It.IsAny<IServerSettingImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((IServerSettingImplementationConfiguration row, string _, string _, string _, CancellationToken _) => Keep(_server, row))
            .ReturnsAsync(GenericResult.Success());
        _tenantProvider.Setup(p => p.Save(It.IsAny<ITenantSettingImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((ITenantSettingImplementationConfiguration row, string _, string _, string _, CancellationToken _) => Keep(_tenants, row))
            .ReturnsAsync(GenericResult.Success());
        _organizationProvider.Setup(p => p.Save(It.IsAny<IOrganizationSettingImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((IOrganizationSettingImplementationConfiguration row, string _, string _, string _, CancellationToken _) => Keep(_organizations, row))
            .ReturnsAsync(GenericResult.Success());
        _scope.Setup(s => s.TenantOf(Finance, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<Guid>.Success(Tenant));
    }

    // A save stores the row it was given, replacing the one of the same name, as the store would.
    private static void Keep<TRow, TConcrete>(List<TConcrete> rows, TRow row)
        where TRow : class
        where TConcrete : class, TRow
    {
        if (row is TConcrete concrete && !rows.Contains(concrete))
        {
            rows.Add(concrete);
        }
    }

    private InheritedSettings Create()
        => new(_serverProvider.Object, _tenantProvider.Object, _organizationProvider.Object, _scope.Object, NullLogger<InheritedSettings>.Instance);

    private void Platform(string name, string value, bool locked = false)
        => _server.Add(new ServerSettingImplementationConfiguration { Name = name, SettingValue = value, DataType = "Int32", IsLocked = locked });

    private void ForTenant(string name, string value, bool locked = false)
        => _tenants.Add(new TenantSettingImplementationConfiguration { Name = $"{Tenant}/{name}", TenantId = Tenant, SettingName = name, SettingValue = value, IsLocked = locked });

    private void ForOrganization(string name, string value)
        => _organizations.Add(new OrganizationSettingImplementationConfiguration
        {
            Name = $"{Finance}/{name}", TenantId = Tenant, OrganizationId = Finance, SettingName = name, SettingValue = value,
        });

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ThePlatformValueAppliesWhenNoLowerLevelSetsOne()
    {
        Platform("MaxRows", "500");

        var view = await Create().Get(SettingScope.ForTenant(Tenant), "MaxRows");

        view.IsSuccess.ShouldBeTrue();
        view.Value!.Value.ShouldBe("500");
        view.Value.SetAt.ShouldBe(SettingScope.PlatformLevel);
        view.Value.IsLocked.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task TheNearestValueWins()
    {
        Platform("MaxRows", "500");
        ForTenant("MaxRows", "300");
        ForOrganization("MaxRows", "100");

        var atTenant = await Create().Get(SettingScope.ForTenant(Tenant), "MaxRows");
        var atOrganization = await Create().Get(SettingScope.ForOrganization(Finance), "MaxRows");

        atTenant.Value!.Value.ShouldBe("300");
        atTenant.Value.SetAt.ShouldBe(SettingScope.TenantLevel);
        atOrganization.Value!.Value.ShouldBe("100");
        atOrganization.Value.SetAt.ShouldBe(SettingScope.OrganizationLevel);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task APlatformLockMakesThePlatformValueGovernAndIgnoresTheTenantValue()
    {
        Platform("MaxRows", "500", locked: true);
        ForTenant("MaxRows", "300");
        ForOrganization("MaxRows", "100");

        var atOrganization = await Create().Get(SettingScope.ForOrganization(Finance), "MaxRows");

        atOrganization.Value!.Value.ShouldBe("500");
        atOrganization.Value.SetAt.ShouldBe(SettingScope.PlatformLevel);
        atOrganization.Value.LockedAt.ShouldBe(SettingScope.PlatformLevel);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ATenantLockMakesTheTenantValueGovernTheOrganizations()
    {
        Platform("MaxRows", "500");
        ForTenant("MaxRows", "300", locked: true);
        ForOrganization("MaxRows", "100");

        var atOrganization = await Create().Get(SettingScope.ForOrganization(Finance), "MaxRows");

        atOrganization.Value!.Value.ShouldBe("300");
        atOrganization.Value.SetAt.ShouldBe(SettingScope.TenantLevel);
        atOrganization.Value.LockedAt.ShouldBe(SettingScope.TenantLevel);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ATenantCannotSetAValueTheTenantLockedAbove()
    {
        Platform("MaxRows", "500", locked: true);

        var set = await Create().Set(SettingScope.ForTenant(Tenant), "MaxRows", "300", false);

        set.IsFailure.ShouldBeTrue();
        _tenantProvider.Verify(p => p.Save(It.IsAny<ITenantSettingImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnOrganizationCannotSetAValueItsTenantLocked()
    {
        Platform("MaxRows", "500");
        ForTenant("MaxRows", "300", locked: true);

        var set = await Create().Set(SettingScope.ForOrganization(Finance), "MaxRows", "100", false);

        set.IsFailure.ShouldBeTrue();
        _organizationProvider.Verify(p => p.Save(It.IsAny<IOrganizationSettingImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ATenantLockHoldsAgainstItsOrganizationsOnlyAndNotThePlatform()
    {
        Platform("MaxRows", "500");
        ForTenant("MaxRows", "300", locked: true);

        var platform = await Create().Set(SettingScope.ForPlatform(), "MaxRows", "400", false);

        platform.IsSuccess.ShouldBeTrue();
        _server.Single().SettingValue.ShouldBe("400");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ATenantSetsItsValueUnderTheNameTheOlderEndpointsUse()
    {
        Platform("MaxRows", "500");

        var set = await Create().Set(SettingScope.ForTenant(Tenant), "MaxRows", "300", true);

        set.IsSuccess.ShouldBeTrue();
        set.Value!.Value.ShouldBe("300");
        set.Value.LockedAt.ShouldBe(SettingScope.TenantLevel);
        var row = _tenants.Single();
        row.Name.ShouldBe($"{Tenant}/MaxRows");
        row.TenantId.ShouldBe(Tenant);
        row.IsLocked.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnOrganizationSetsItsValueInItsTenantsScope()
    {
        Platform("MaxRows", "500");

        var set = await Create().Set(SettingScope.ForOrganization(Finance), "MaxRows", "100", false);

        set.IsSuccess.ShouldBeTrue();
        var row = _organizations.Single();
        row.OrganizationId.ShouldBe(Finance);
        row.TenantId.ShouldBe(Tenant);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnOrganizationCannotLock()
    {
        Platform("MaxRows", "500");

        var set = await Create().Set(SettingScope.ForOrganization(Finance), "MaxRows", "100", true);

        set.IsFailure.ShouldBeTrue();
        _organizations.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ThePlatformLiftsATenantsLockAndTheValueStays()
    {
        Platform("MaxRows", "500");
        ForTenant("MaxRows", "300", locked: true);

        var lifted = await Create().Lift(SettingScope.ForTenant(Tenant), "MaxRows");

        lifted.IsSuccess.ShouldBeTrue();
        lifted.Value!.IsLocked.ShouldBeFalse();
        lifted.Value.Value.ShouldBe("300");
        _tenants.Single().IsLocked.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task LiftingALockThatIsNotHeldFails()
    {
        Platform("MaxRows", "500");
        ForTenant("MaxRows", "300");

        var lifted = await Create().Lift(SettingScope.ForTenant(Tenant), "MaxRows");

        lifted.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASettingThePlatformDoesNotDefineIsNotFoundBelowThePlatform()
    {
        var set = await Create().Set(SettingScope.ForTenant(Tenant), "Nothing", "1", false);

        set.IsFailure.ShouldBeTrue();
        _tenants.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnOrganizationTheCallerDoesNotActInIsRefusedBeforeAnythingIsRead()
    {
        var other = Guid.NewGuid();
        _scope.Setup(s => s.TenantOf(other, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid>.Failure(new GenericMessage("the caller does not act in the organization")));

        var listed = await Create().List(SettingScope.ForOrganization(other));

        listed.IsFailure.ShouldBeTrue();
        _serverProvider.Verify(p => p.Get(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task TheListHoldsEachActiveSettingWithWhereItIsSet()
    {
        Platform("MaxRows", "500");
        Platform("MaxDepth", "5", locked: true);
        ForTenant("MaxRows", "300");

        var listed = await Create().List(SettingScope.ForTenant(Tenant));

        listed.Value!.Count.ShouldBe(2);
        listed.Value.Single(s => s.Name == "MaxRows").SetAt.ShouldBe(SettingScope.TenantLevel);
        listed.Value.Single(s => s.Name == "MaxDepth").LockedAt.ShouldBe(SettingScope.PlatformLevel);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheEffectiveProviderStopsAtALockedPlatformValue()
    {
        var tenantRows = new List<ITenantSettingImplementationConfiguration>
        {
            new TenantSettingImplementationConfiguration { TenantId = Tenant, SettingName = "MaxRows", SettingValue = "300" },
        };
        var server = new Mock<IServerSettingConfigurationProvider>();
        server.Setup(p => p.Get("MaxRows", It.IsAny<CancellationToken>())).ReturnsAsync(
            GenericResult<IServerSettingImplementationConfiguration>.Success(new ServerSettingImplementationConfiguration { Name = "MaxRows", SettingValue = "500", DataType = "Int32", IsLocked = true }));
        var tenant = new Mock<ITenantSettingConfigurationProvider>();
        tenant.Setup(p => p.Get(It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IReadOnlyList<ITenantSettingImplementationConfiguration>>.Success(tenantRows));
        var role = new Mock<IRoleSettingConfigurationProvider>();
        var provider = new DefaultEffectiveSettingsProvider(server.Object, tenant.Object, role.Object, NullLogger<DefaultEffectiveSettingsProvider>.Instance);

        provider.GetEffectiveValue<int>("MaxRows", Tenant).ShouldBe(500);
    }
}
