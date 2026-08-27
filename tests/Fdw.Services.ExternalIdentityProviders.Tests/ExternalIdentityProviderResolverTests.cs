using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Fdw.Services.Data;

namespace Fdw.Services.ExternalIdentityProviders.Tests;

/// <summary>
/// <see cref="ExternalIdentityProviderResolver"/>'s resolution rule: resolve by the caller-supplied
/// provider name; else the single active configuration; else fail loud — an unknown name, no active
/// configuration, or several active with no name to disambiguate. Never guesses among multiple.
/// </summary>
public sealed class ExternalIdentityProviderResolverTests
{
    private static ExternalIdentityProviderConfiguration Header(string name)
        => new() { Id = Guid.NewGuid(), Name = name };

    private static Mock<IExternalIdentityProviderServiceProvider> ServiceProvider()
        => new();

    // Why: the resolver depends on the concrete configuration provider (its Get(ct) is virtual). Mock it
    // over a dummy gateway Lazy that is never dereferenced because Get is stubbed.
    private static Mock<ExternalIdentityProviderConfigurationProvider> ConfigProvider()
        => new(
            NullLogger<ExternalIdentityProviderConfigurationProvider>.Instance,
            new ConfigurationGatewayProvider(),
            "ConfigurationDb", "auth");

    private static ExternalIdentityProviderResolver Resolver(
        Mock<IExternalIdentityProviderServiceProvider> serviceProvider,
        Mock<ExternalIdentityProviderConfigurationProvider> configProvider)
        => new(serviceProvider.Object, configProvider.Object, NullLogger<ExternalIdentityProviderResolver>.Instance);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ResolvesByExplicitName()
    {
        var expected = Mock.Of<IExternalIdentityProvider>();
        var serviceProvider = ServiceProvider();
        serviceProvider.Setup(x => x.Get("azure", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IExternalIdentityProvider>.Success(expected));

        var result = await Resolver(serviceProvider, ConfigProvider()).Resolve("azure", Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task FailsWhenNamedProviderNotFound()
    {
        var serviceProvider = ServiceProvider();
        serviceProvider.Setup(x => x.Get("ghost", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IExternalIdentityProvider>.Success(null!));

        var result = await Resolver(serviceProvider, ConfigProvider()).Resolve("ghost", Ct);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task ResolvesSoleActiveWhenNoNameGiven()
    {
        var expected = Mock.Of<IExternalIdentityProvider>();
        var configProvider = ConfigProvider();
        configProvider.Setup(x => x.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<ExternalIdentityProviderConfiguration>>.Success(new[] { Header("only") }));
        var serviceProvider = ServiceProvider();
        serviceProvider.Setup(x => x.Get("only", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IExternalIdentityProvider>.Success(expected));

        var result = await Resolver(serviceProvider, configProvider).Resolve(null, Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task FailsWhenNoActiveProviders()
    {
        var configProvider = ConfigProvider();
        configProvider.Setup(x => x.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<ExternalIdentityProviderConfiguration>>.Success(Array.Empty<ExternalIdentityProviderConfiguration>()));

        var result = await Resolver(ServiceProvider(), configProvider).Resolve(null, Ct);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task FailsWhenMultipleActiveAndNoNameGiven()
    {
        var configProvider = ConfigProvider();
        configProvider.Setup(x => x.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<ExternalIdentityProviderConfiguration>>.Success(new[] { Header("a"), Header("b") }));

        var result = await Resolver(ServiceProvider(), configProvider).Resolve(null, Ct);

        result.IsSuccess.ShouldBeFalse();
    }
}
