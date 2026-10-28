using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Multitenancy.Endpoints.Tests;

/// <summary>
/// Tests that the feature service decides who may act for features inside the service (FDW-2474, FDW-2478, FDW-2887): each organization call
/// asks the organization scope for its right and each company call asks the scope authority at the company's scope, before it touches the
/// store; and a refused caller reads and writes nothing. What makes the organization scope refuse is OrganizationScopeTests' question.
/// </summary>
[Trait("Category", "Security")]
public sealed class FeatureScopeTests
{
    private static readonly Guid Tenant = RetentionTestScope.Tenant;
    private static readonly Guid Finance = RetentionTestScope.Finance;
    private static readonly Guid Unknown = RetentionTestScope.Unknown;
    private static readonly FeatureTables Tables = new("PlatformConfiguration", "tenant", "TenantFeatures", "OrganizationFeatures");

    private sealed class Rig(bool allows)
    {
        internal Mock<IConfigurationGatewayProvider> Gateways { get; } = new();

        internal Mock<IOrganizationScope> Scope { get; } = RetentionTestScope.Scope(allows);

        internal Mock<IScopeAuthority> Authority { get; } = RetentionTestScope.Authority(allows);

        internal FeatureEnablement Service => new(
            Gateways.Object, Tables, Scope.Object, Authority.Object, NullLogger<FeatureEnablement>.Instance);

        internal void NothingRead() => Gateways.Verify(g => g.Get(It.IsAny<string>()), Times.Never);

        internal void Asked(string right, int times = 1)
            => Scope.Verify(s => s.TenantOf(right, Finance, It.IsAny<CancellationToken>()), Times.Exactly(times));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AReaderOfAnOrganizationsFeaturesIsAskedForTheOrganizationsReadRightAtTheOrganization()
    {
        var rig = new Rig(false);

        var listed = await rig.Service.ForOrganization(Finance, TestContext.Current.CancellationToken);

        listed.IsFailure.ShouldBeTrue();
        rig.Asked("organizations:read");
        rig.NothingRead();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASwitchOfAnOrganizationsFeatureIsAskedForTheOrganizationsWriteRightAndAChallengedCallerWritesNothing()
    {
        var rig = new Rig(false);

        var set = await rig.Service.SetForOrganization(Finance, "Documents", true, TestContext.Current.CancellationToken);

        set.IsFailure.ShouldBeTrue();
        rig.Asked("organizations:write");
        rig.NothingRead();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AReaderOfACompanysFeaturesIsAskedForTheTenantsReadRightAtTheCompanyWithNoOrganization()
    {
        var rig = new Rig(false);

        var listed = await rig.Service.ForTenant(Tenant, TestContext.Current.CancellationToken);

        listed.IsFailure.ShouldBeTrue();
        rig.Authority.Verify(a => a.May("tenants:read", new ThingScope(Tenant, null), It.IsAny<CancellationToken>()), Times.Once);
        rig.NothingRead();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASwitchOfACompanysFeatureIsAskedForTheTenantsWriteRightAndAChallengedCallerWritesNothing()
    {
        var rig = new Rig(false);

        var set = await rig.Service.SetForTenant(Tenant, "Documents", true, TestContext.Current.CancellationToken);

        set.IsFailure.ShouldBeTrue();
        rig.Authority.Verify(a => a.May("tenants:write", new ThingScope(Tenant, null), It.IsAny<CancellationToken>()), Times.Once);
        rig.NothingRead();
    }
}
