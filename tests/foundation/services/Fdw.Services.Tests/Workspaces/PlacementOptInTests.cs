using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Calculations;
using Fdw.Services.Configuration;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Workspaces.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces;

/// <summary>
/// FDW-3692: the providers whose members sit in a workspace declare it, under a kind the workspace domain knows; a host that registers no
/// placement gets a stand-in that refuses every create and says why, and one that registers a placement gets that placement.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public sealed class PlacementOptInTests
{
    private static IConfigurationPlacement Placing() => new Mock<IConfigurationPlacement>().Object;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Workspaces")]
    public void TheDataStoreProviderPlacesItsMembersAsADataStore()
    {
        var provider = new DataStoreConfigurationProvider(
            NullLogger<DataStoreConfigurationProvider>.Instance, Mock.Of<IConfigurationGatewayProvider>(), "PlatformConfiguration", "data", "DataStore", Placing(), Mock.Of<Fdw.Services.Authentication.Abstractions.Security.IAuthenticationContextAccessor>(), Mock.Of<Fdw.Services.Multitenancy.Abstractions.ICrossTenantChangePolicy>());

        WorkspaceResourceKinds.ByName(provider.PlacementKind).ShouldNotBeSameAs(WorkspaceResourceKinds.NotFound);
        provider.PlacementKind.ShouldBe("DataStore");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Workspaces")]
    public void TheCalculationProviderPlacesItsMembersAsACalculation()
    {
        var provider = new CalculationConfigurationProvider(
            NullLogger<CalculationConfigurationProvider>.Instance, Mock.Of<IConfigurationGatewayProvider>(), "PlatformConfiguration", "calc", "CalculationEntity", Placing());

        WorkspaceResourceKinds.ByName(provider.PlacementKind).ShouldNotBeSameAs(WorkspaceResourceKinds.NotFound);
        provider.PlacementKind.ShouldBe("Calculation");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Workspaces")]
    public void AProviderWillNotTakeNoPlacement()
    {
        Should.Throw<ArgumentNullException>(() => new DataStoreConfigurationProvider(
            NullLogger<DataStoreConfigurationProvider>.Instance, Mock.Of<IConfigurationGatewayProvider>(), "PlatformConfiguration", "data", "DataStore", null!, Mock.Of<Fdw.Services.Authentication.Abstractions.Security.IAuthenticationContextAccessor>(), Mock.Of<Fdw.Services.Multitenancy.Abstractions.ICrossTenantChangePolicy>()));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Workspaces")]
    public async Task AHostWithNoPlacementRefusesEveryCreateAndNamesWhatIsMissing()
    {
        var placement = ConfigurationPlacementSource.From(new ServiceCollection().BuildServiceProvider());

        placement.ShouldBeOfType<UnavailablePlacement>();
        var placed = await placement.Place(new PlacementRequest("DataSet", Guid.NewGuid(), "NflTeams"), null!, TestContext.Current.CancellationToken);

        placed.IsFailure.ShouldBeTrue();
        placed.Messages.Select(message => message.Code).ShouldContain("SERVICES-21711");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Workspaces")]
    public void AHostThatRegisteredAPlacementGetsThatPlacement()
    {
        var registered = Placing();
        var services = new ServiceCollection().AddSingleton(registered).BuildServiceProvider();

        ConfigurationPlacementSource.From(services).ShouldBeSameAs(registered);
    }
}
