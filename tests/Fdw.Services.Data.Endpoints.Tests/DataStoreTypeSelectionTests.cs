using System;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.TestDouble;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// A data store's type comes from what its connection's kind offers, limited to the data store types this deployment registered.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Configuration")]
public sealed class DataStoreTypeSelectionTests
{
    private const string ProbeStoreName = DataStoreTypeSelectionProbes.ProbeStoreName;
    private const string AbsentStoreName = DataStoreTypeSelectionProbes.AbsentStoreName;

    private static CreateDataStoreRequest Request(string? storeType = null) => new() { Name = "Files", ConnectionName = "c", StoreType = storeType };

    [Fact]
    public void AKindOffersOnlyTheDataStoreTypesThisDeploymentRegistered()
    {
        ConnectionTypes.ByName("ProbeTransport").SupportedDataStoreTypes.ShouldBe([ProbeStoreName]);
    }

    [Fact]
    public void AKindWithNoDeclarationAndNoDataStoreTypeOfItsOwnNameOffersNothing()
    {
        ConnectionTypes.ByName("ProbeNoStores").SupportedDataStoreTypes.ShouldBeEmpty();
    }

    [Fact]
    public void AStoreTakesTheKindsFirstOfferedTypeWhenTheRequestNamesNone()
    {
        var chosen = DataStoreTypeSelection.Select(Request(), "ProbeTransport", NullLogger.Instance);

        chosen.IsSuccess.ShouldBeTrue();
        chosen.Value.ShouldBe(ProbeStoreName);
    }

    [Fact]
    public void ARequestMayNameAnOfferedTypeInAnyCase()
    {
        var chosen = DataStoreTypeSelection.Select(Request("probefiles"), "ProbeTransport", NullLogger.Instance);

        chosen.IsSuccess.ShouldBeTrue();
        chosen.Value.ShouldBe(ProbeStoreName);
    }

    [Fact]
    public void ARequestForATypeTheKindDoesNotOfferIsRefused()
    {
        DataStoreTypeSelection.Select(Request("MsSql"), "ProbeTransport", NullLogger.Instance).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void ARequestForATypeThisDeploymentDidNotRegisterIsRefusedEvenWhenTheKindDeclaresIt()
    {
        DataStoreTypeSelection.Select(Request(AbsentStoreName), "ProbeTransport", NullLogger.Instance).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void AKindThatOffersNothingRefusesAStoreWithAClearFailure()
    {
        DataStoreTypeSelection.Select(Request(), "ProbeNoStores", NullLogger.Instance).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void AKindThatIsNotRegisteredKeepsTheConnectionsImplementationName()
    {
        var chosen = DataStoreTypeSelection.Select(Request(), "SomethingElse", NullLogger.Instance);

        chosen.IsSuccess.ShouldBeTrue();
        chosen.Value.ShouldBe("SomethingElse");
    }
}
