using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Users;
using Fdw.Services.Users.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Users.Tests;

/// <summary>
/// Unit tests for <see cref="UserTenantConfigurationProvider"/>.
///
/// Only <see cref="IConfigurationGateway"/> is faked. The real provider runs under test.
/// </summary>
public class UserTenantConfigurationProviderTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static UserTenantConfigurationProvider MakeProvider(
        Mock<IConfigurationGateway>? gateway = null,
        params UserTenantConfiguration[] storedRows)
    {

        var gw = gateway ?? new Mock<IConfigurationGateway>();

        if (gateway is null)
        {
            gw.Setup(g => g.Execute<IEnumerable<UserTenantConfiguration>>(
                    It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(GenericResult<IEnumerable<UserTenantConfiguration>>.Success(storedRows));
        }

        return new UserTenantConfigurationProvider(
            NullLogger<UserTenantConfigurationProvider>.Instance,
            new Lazy<IConfigurationGateway>(() => gw.Object));
    }

    private static UserTenantConfiguration Membership(
        Guid userId,
        Guid tenantId,
        bool isDefault = false)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = $"{userId}:{tenantId}",
            UserId = userId,
            TenantId = tenantId,
            IsDefault = isDefault,
            IsCurrent = true,
            IsDeleted = false,
        };

    // ── GetUserTenants ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task GetUserTenantsReturnsTenantsForUser()
    {
        var userId = Guid.NewGuid();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var gw = new Mock<IConfigurationGateway>();
        // Why: IConfigurationGateway.DataStores is contractually non-null; ResolveParentJoin reads it.
        gw.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());
        gw.Setup(g => g.Execute<IEnumerable<UserTenantConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(GenericResult<IEnumerable<UserTenantConfiguration>>.Success(new[]
          {
              Membership(userId, tenantA),
              Membership(userId, tenantB),
          }));

        var provider = MakeProvider(gw);

        var result = await provider.GetUserTenants(userId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(2);
        result.Value.ShouldContain(tenantA);
        result.Value.ShouldContain(tenantB);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Users")]
    public async Task GetUserTenantsReturnsEmptyWhenNoMemberships()
    {
        var gw = new Mock<IConfigurationGateway>();
        // Why: IConfigurationGateway.DataStores is contractually non-null; ResolveParentJoin reads it.
        gw.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());
        gw.Setup(g => g.Execute<IEnumerable<UserTenantConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(GenericResult<IEnumerable<UserTenantConfiguration>>.Success(
              Enumerable.Empty<UserTenantConfiguration>()));

        var provider = MakeProvider(gw);

        var result = await provider.GetUserTenants(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBeEmpty();
    }

    // ── GetDefaultTenant ──────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task GetDefaultTenantReturnsDefaultTenantId()
    {
        var userId = Guid.NewGuid();
        var defaultTenantId = Guid.NewGuid();

        var gw = new Mock<IConfigurationGateway>();
        // Why: IConfigurationGateway.DataStores is contractually non-null; ResolveParentJoin reads it.
        gw.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());
        gw.Setup(g => g.Execute<IEnumerable<UserTenantConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(GenericResult<IEnumerable<UserTenantConfiguration>>.Success(new[]
          {
              Membership(userId, defaultTenantId, isDefault: true),
          }));

        var provider = MakeProvider(gw);

        var result = await provider.GetDefaultTenant(userId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBe(defaultTenantId);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task GetDefaultTenantReturnsNullValueWhenNoDefaultRow()
    {
        // Why: a user with no default tenant row is a valid state immediately after creation
        // (before GrantTenantAccess runs). The provider MUST return Success(null) — callers
        // handle the absence; it is NOT a failure to have no default tenant row.
        var gw = new Mock<IConfigurationGateway>();
        // Why: IConfigurationGateway.DataStores is contractually non-null; ResolveParentJoin reads it.
        gw.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());
        gw.Setup(g => g.Execute<IEnumerable<UserTenantConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(GenericResult<IEnumerable<UserTenantConfiguration>>.Success(
              Enumerable.Empty<UserTenantConfiguration>()));

        var provider = MakeProvider(gw);

        var result = await provider.GetDefaultTenant(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    // ── GrantTenantAccess ─────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task GrantTenantAccessSavesNewRow()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var gw = new Mock<IConfigurationGateway>();
        // Why: IConfigurationGateway.DataStores is contractually non-null; ResolveParentJoin reads it.
        gw.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        // Why: GrantTenantAccess uses an Insert command that routes through Execute<int>.
        // Return 1 (one row inserted) to indicate success.
        gw.Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(GenericResult<int>.Success(1));

        var provider = MakeProvider(gw);

        var result = await provider.GrantTenantAccess(
            userId, tenantId, isDefault: true, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        gw.Verify(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task GrantTenantAccessReturnsFailureWhenGatewayFails()
    {
        var gw = new Mock<IConfigurationGateway>();
        // Why: IConfigurationGateway.DataStores is contractually non-null; ResolveParentJoin reads it.
        gw.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());
        gw.Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(GenericResult<int>.Failure());

        var provider = MakeProvider(gw);

        var result = await provider.GrantTenantAccess(
            Guid.NewGuid(), Guid.NewGuid(), isDefault: false, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }
}
