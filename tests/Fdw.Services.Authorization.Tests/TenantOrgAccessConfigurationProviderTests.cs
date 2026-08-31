using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// Unit tests for <see cref="TenantOrgAccessConfigurationProvider"/>.
///
/// Only <see cref="IConfigurationGateway"/> is faked. The real provider runs under test,
/// including the three-parameter (userId, tenantId, orgId) query filter.
/// </summary>
[Trait("Priority", "P1")]
public class TenantOrgAccessConfigurationProviderTests
{
    // The provider selects its own gateway now, so the fake reaches it through a gateway provider.
    private static TenantOrgAccessConfigurationProvider MakeProvider(Mock<IConfigurationGateway> gateway)
    {
        var gatewayProvider = new Mock<IConfigurationGatewayProvider>();
        gatewayProvider
            .Setup(p => p.Get(It.IsAny<string>()))
            .Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));

        return new TenantOrgAccessConfigurationProvider(
            gatewayProvider.Object,
            NullLogger<TenantOrgAccessConfigurationProvider>.Instance);
    }

    // ── Get ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_WhenGatewayReturnsGrants_ReturnsGrantList()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var gateway = new Mock<IConfigurationGateway>();
        gateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        gateway.Setup(g => g.Execute<IEnumerable<TenantOrgAccessConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<TenantOrgAccessConfiguration>>.Success([
                new TenantOrgAccessConfiguration { UserId = userId, TenantId = tenantId, OrgId = orgId, RoleName = "Operator" },
                new TenantOrgAccessConfiguration { UserId = userId, TenantId = tenantId, OrgId = orgId, PermissionName = "data:read" }
            ]));

        var provider = MakeProvider(gateway);

        var result = await provider.Get(userId, tenantId, orgId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(2);
        result.Value[0].RoleName.ShouldBe("Operator");
        result.Value[1].PermissionName.ShouldBe("data:read");
    }

    [Fact]
    public async Task Get_WhenNoGrants_ReturnsEmptyList()
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        gateway.Setup(g => g.Execute<IEnumerable<TenantOrgAccessConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<TenantOrgAccessConfiguration>>.Success([]));

        var provider = MakeProvider(gateway);

        var result = await provider.Get(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Get_WhenGatewayFails_ReturnsFailure()
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        gateway.Setup(g => g.Execute<IEnumerable<TenantOrgAccessConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<TenantOrgAccessConfiguration>>.Failure(new GenericMessage("DB offline")));

        var provider = MakeProvider(gateway);

        var result = await provider.Get(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }
}
