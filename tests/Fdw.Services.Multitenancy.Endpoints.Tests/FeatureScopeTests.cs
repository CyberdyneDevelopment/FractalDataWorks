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
/// Tests that the feature service decides who may act for features inside the service (FDW-2474, FDW-2478): each organization call asks the
/// scope authority for its right at the organization's own scope and each company call at the company's, before it touches the store; an
/// organization the store does not hold is refused in the forbidden category; and a refused caller reads and writes nothing.
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

        internal Mock<IScopeAuthority> Authority { get; } = RetentionTestScope.Authority(allows);

        internal FeatureEnablement Service => new(
            Gateways.Object, Tables, RetentionTestScope.Organizations().Object, Authority.Object, NullLogger<FeatureEnablement>.Instance);

        internal void NothingRead() => Gateways.Verify(g => g.Get(It.IsAny<string>()), Times.Never);

        internal void Asked(string right, int times = 1)
            => Authority.Verify(a => a.May(right, new ThingScope(Tenant, Finance), It.IsAny<CancellationToken>()), Times.Exactly(times));
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
    public async Task AnOrganizationTheStoreDoesNotHoldIsRefusedInTheForbiddenCategoryWithoutAskingTheAuthority()
    {
        var rig = new Rig(true);

        var listed = await rig.Service.ForOrganization(Unknown, TestContext.Current.CancellationToken);
        var set = await rig.Service.SetForOrganization(Unknown, "Documents", true, TestContext.Current.CancellationToken);

        listed.IsFailure.ShouldBeTrue();
        set.IsFailure.ShouldBeTrue();
        FailureCategory.Of(listed).ShouldBe(ResultCategories.ByName("Forbidden"));
        FailureCategory.Of(set).ShouldBe(ResultCategories.ByName("Forbidden"));
        rig.Authority.Verify(a => a.May(It.IsAny<string>(), It.IsAny<ThingScope>(), It.IsAny<CancellationToken>()), Times.Never);
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
