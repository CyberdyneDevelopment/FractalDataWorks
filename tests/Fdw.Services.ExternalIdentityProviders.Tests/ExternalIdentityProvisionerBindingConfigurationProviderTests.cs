using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Binding;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.ExternalIdentityProviders.Tests;

/// <summary>
/// Behavior of <see cref="ExternalIdentityProvisionerBindingConfigurationProvider.ResolveProvisionerName"/>:
/// exact (TenantId, ProviderName) equality — including null==null for the global row — with NO
/// tenant-to-global fall-through; absent is a legitimate Success(null); more than one current match is a
/// fail-loud ambiguity.
/// </summary>
public sealed class ExternalIdentityProvisionerBindingConfigurationProviderTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static ExternalIdentityProvisionerBindingConfigurationProvider BuildSut(
        params ExternalIdentityProvisionerBindingConfiguration[] rows)
    {
        var gatewayMock = new Mock<IConfigurationGateway>(MockBehavior.Strict);
        gatewayMock
            .Setup(g => g.Execute<IEnumerable<ExternalIdentityProvisionerBindingConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ExternalIdentityProvisionerBindingConfiguration>>.Success(rows));

        return new ExternalIdentityProvisionerBindingConfigurationProvider(
            NullLogger<ExternalIdentityProvisionerBindingConfigurationProvider>.Instance,
            new Lazy<IConfigurationGateway>(() => gatewayMock.Object));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ExactTenantMatchResolvesToItsProvisioner()
    {
        var tenantId = Guid.NewGuid();
        var sut = BuildSut(new ExternalIdentityProvisionerBindingConfiguration
        {
            TenantId = tenantId,
            ProviderName = "p1",
            ProvisionerName = "ProvA",
        });

        var result = await sut.ResolveProvisionerName(tenantId, "p1", Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("ProvA");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task GlobalMatchResolvesWhenTenantIdIsExplicitlyNull()
    {
        var sut = BuildSut(new ExternalIdentityProvisionerBindingConfiguration
        {
            TenantId = null,
            ProviderName = "p1",
            ProvisionerName = "ProvGlobal",
        });

        var result = await sut.ResolveProvisionerName(tenantId: null, providerName: "p1", Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("ProvGlobal");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AbsentBindingResolvesToSuccessNull()
    {
        var sut = BuildSut(); // no rows at all

        var result = await sut.ResolveProvisionerName(Guid.NewGuid(), "unbound-provider", Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task TenantScopedLookupDoesNotFallThroughToGlobalBinding()
    {
        // Why: only the global (TenantId == null) binding exists for this provider — a tenant-scoped
        // lookup MUST NOT silently fall through to it. NO FALLBACKS WITHOUT EXPLICIT APPROVAL.
        var sut = BuildSut(new ExternalIdentityProvisionerBindingConfiguration
        {
            TenantId = null,
            ProviderName = "p1",
            ProvisionerName = "ProvGlobal",
        });

        var result = await sut.ResolveProvisionerName(Guid.NewGuid(), "p1", Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task MoreThanOneCurrentMatchFailsLoud()
    {
        var tenantId = Guid.NewGuid();
        var sut = BuildSut(
            new ExternalIdentityProvisionerBindingConfiguration { TenantId = tenantId, ProviderName = "p1", ProvisionerName = "ProvA" },
            new ExternalIdentityProvisionerBindingConfiguration { TenantId = tenantId, ProviderName = "p1", ProvisionerName = "ProvB" });

        var result = await sut.ResolveProvisionerName(tenantId, "p1", Ct);

        result.IsSuccess.ShouldBeFalse();
    }
}
