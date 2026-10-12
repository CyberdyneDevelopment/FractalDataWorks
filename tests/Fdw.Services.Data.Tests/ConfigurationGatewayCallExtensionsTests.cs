using System;
using System.Threading.Tasks;
using Fdw.Services.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// The AuthDb stores reach their tables through the configuration gateway of a declared connection (FDW-1084). A
/// connection the host does not declare is the gateway provider's refusal, carried through unchanged.
/// </summary>
public sealed class ConfigurationGatewayCallExtensionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AnUndeclaredConnectionCarriesTheProvidersRefusal()
    {
        var gateways = new ConfigurationGatewayProvider();

        var result = await gateways.Execute<int>("AuthDb", default(DataGatewayCall), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task ANullProviderIsRefusedAtTheCall()
    {
        IConfigurationGatewayProvider gateways = null!;

        await Should.ThrowAsync<ArgumentNullException>(
            () => gateways.Execute<int>("AuthDb", default(DataGatewayCall), TestContext.Current.CancellationToken));
    }
}
