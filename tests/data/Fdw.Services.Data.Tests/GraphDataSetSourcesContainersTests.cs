using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>FDW-5408: where a data store holds the containers a graph names is read once for the store, from its stored rows narrowed on the container names.</summary>
public sealed class GraphDataSetSourcesContainersTests
{
    private static Mock<IDataStoreConfigurationProvider> Store(Action<Mock<IDataStoreImplementationConfiguration>> shape)
    {
        var store = new Mock<IDataStoreImplementationConfiguration>();
        shape(store);
        var provider = new Mock<IDataStoreConfigurationProvider>();
        provider
            .Setup(p => p.Get("Crm", It.IsAny<int>(), It.IsAny<ChildNarrowing>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStoreImplementationConfiguration>.Success(store.Object));
        return provider;
    }

    private static GraphDataSetSources Over(Mock<IDataStoreConfigurationProvider> stores) =>
        new(new Mock<Fdw.Data.DataSets.Abstractions.IDataSetConfigurationProvider>().Object, stores.Object);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5408")]
    public async Task EachContainerTheStoreHoldsIsAnsweredWithItsPathAndTheStoreIsReadOnceNarrowedOnTheNames()
    {
        var stores = Store(store => store.SetupGet(s => s.Paths).Returns(new List<DataPathConfiguration>
        {
            new() { Name = "dbo", Containers = [new DataContainerConfiguration { Name = "Customers" }, new DataContainerConfiguration { Name = "Orders" }] },
            new() { Name = "sales", Containers = [new DataContainerConfiguration { Name = "Invoices" }] },
        }));

        var answer = await Over(stores).Containers("Crm", ["customers", "Invoices", "Customers"], TestContext.Current.CancellationToken);

        answer.IsSuccess.ShouldBeTrue(answer.CurrentMessage);
        string.Join(",", answer.Value!["CUSTOMERS"]).ShouldBe("dbo");
        string.Join(",", answer.Value["Invoices"]).ShouldBe("sales");
        stores.Verify(
            p => p.Get(
                "Crm", ConfigurationLevels.WholeAggregate,
                It.Is<ChildNarrowing>(n => n.ChildType == typeof(DataContainerConfiguration) && n.Property == "Name" && n.Values.Count == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5408")]
    public async Task ANameTheStoreHoldsInTwoPathsIsAnsweredWithBothAndNoneIsPicked()
    {
        var stores = Store(store => store.SetupGet(s => s.Paths).Returns(new List<DataPathConfiguration>
        {
            new() { Name = "dbo", Containers = [new DataContainerConfiguration { Name = "Customers" }] },
            new() { Name = "archive", Containers = [new DataContainerConfiguration { Name = "Customers" }] },
        }));

        var answer = await Over(stores).Containers("Crm", ["Customers"], TestContext.Current.CancellationToken);

        answer.IsSuccess.ShouldBeTrue(answer.CurrentMessage);
        string.Join(",", answer.Value!["Customers"].OrderBy(path => path, StringComparer.Ordinal)).ShouldBe("archive,dbo");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5408")]
    public async Task AStoreThatIsNotThereAnswersNoContainerAndNotAFailure()
    {
        var stores = new Mock<IDataStoreConfigurationProvider>();
        stores
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<ChildNarrowing>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStoreImplementationConfiguration>.Success(default!));

        var answer = await Over(stores).Containers("Crm", ["Customers"], TestContext.Current.CancellationToken);

        answer.IsSuccess.ShouldBeTrue(answer.CurrentMessage);
        answer.Value!.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5408")]
    public async Task AReadThatFailsForAnotherReasonThanTheStoreBeingAbsentIsTheAnswer()
    {
        var stores = new Mock<IDataStoreConfigurationProvider>();
        stores
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<ChildNarrowing>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStoreImplementationConfiguration>.Failure(
                DataServiceResultCodes.ByName("GraphDataSetNotReadable"), NullLogger.Instance, ResultDetails.Create("dataSet", "Crm")));

        var answer = await Over(stores).Containers("Crm", ["Customers"], TestContext.Current.CancellationToken);

        answer.IsFailure.ShouldBeTrue();
    }
}
