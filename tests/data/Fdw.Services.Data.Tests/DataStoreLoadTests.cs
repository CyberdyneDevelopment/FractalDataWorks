using System;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// The DataStore domain's load at startup (FDW-3863): a data store that cannot be composed from its rows fails the load, naming why, and is not kept as the header row alone. Before this
/// the load logged the failure and returned, so the host started with no data stores and the error was only in the journal.
/// </summary>
public sealed class DataStoreLoadTests
{
    private static IServiceProvider Services(Mock<IConfigurationGatewayProvider> gateways) =>
        new ServiceCollection()
            .AddSingleton(new DataStoreConfigurationProvider(
                NullLogger<DataStoreConfigurationProvider>.Instance, gateways.Object, "ConfigurationDb", "data", "DataStoreConfiguration", new Mock<IConfigurationPlacement>().Object,
                Mock.Of<Fdw.Services.Authentication.Abstractions.Security.IAuthenticationContextAccessor>(), Mock.Of<Fdw.Services.Multitenancy.Abstractions.ICrossTenantChangePolicy>()))
            .BuildServiceProvider();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public async Task DataStoresThatCannotBeReadFailTheLoadAndTheFailureIsReturned()
    {
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(g => g.Get(It.IsAny<string>())).Returns(GenericResult<IConfigurationGateway>.Failure(new GenericMessage("the configuration store is not reachable")));

        var loaded = await ConfigurationGatewayDataStoreProvider.LoadStores(Services(gateways), NullLogger<ConfigurationGatewayDataStoreProvider>.Instance);

        loaded.IsFailure.ShouldBeTrue();
        loaded.CurrentMessage.ShouldNotBeNull().ShouldContain("the configuration store is not reachable");
    }
}
