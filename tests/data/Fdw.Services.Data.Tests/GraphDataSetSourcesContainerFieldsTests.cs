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

/// <summary>FDW-5523: the fields of a container are read from the data store's stored rows, narrowed on the container's name, and no container is picked for the caller.</summary>
public sealed class GraphDataSetSourcesContainerFieldsTests
{
    private static Mock<IDataStoreConfigurationProvider> Store(params DataPathConfiguration[] paths)
    {
        var store = new Mock<IDataStoreImplementationConfiguration>();
        store.SetupGet(s => s.Paths).Returns(paths.ToList());
        var provider = new Mock<IDataStoreConfigurationProvider>();
        provider
            .Setup(p => p.Get("Crm", It.IsAny<int>(), It.IsAny<ChildNarrowing>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStoreImplementationConfiguration>.Success(store.Object));
        return provider;
    }

    private static GraphDataSetSources Over(Mock<IDataStoreConfigurationProvider> stores) =>
        new(new Mock<Fdw.Data.DataSets.Abstractions.IDataSetConfigurationProvider>().Object, stores.Object);

    private static DataContainerConfiguration Container(string name, params DataContainerFieldConfiguration[] fields) => new() { Name = name, Fields = [.. fields] };

    private static DataContainerFieldConfiguration Field(string name, int ordinal, string? type, bool isDeleted = false) =>
        new() { Name = name, Ordinal = ordinal, FrameworkDataType = type, IsDeleted = isDeleted };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5523")]
    public async Task TheFieldsComeInTheContainersOrderWithTheDeletedOnesLeftOutAndAnUnstatedTypeStaysNone()
    {
        var stores = Store(new DataPathConfiguration
        {
            Name = "dbo",
            Containers = [Container("Customers", Field("Name", 2, "String"), Field("Id", 1, "Int32"), Field("Old", 3, "String", isDeleted: true), Field("Note", 4, null))],
        });

        var answer = await Over(stores).ContainerFields("src", "Crm", null, "customers", NullLogger.Instance, TestContext.Current.CancellationToken);

        answer.IsSuccess.ShouldBeTrue(answer.CurrentMessage);
        var fields = answer.Value.ShouldNotBeNull();
        fields.Select(field => field.Name).ShouldBe(["Id", "Name", "Note"]);
        fields.Select(field => field.TypeName).ShouldBe(["Int32", "String", null]);
        stores.Verify(
            p => p.Get(
                "Crm", ConfigurationLevels.WholeAggregate,
                It.Is<ChildNarrowing>(n => n.ChildType == typeof(DataContainerConfiguration) && n.Property == "Name" && n.Values.Count == 1 && n.Values[0] == "customers"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5523")]
    public async Task AStatedPathPicksThePathOfAContainerNameTheStoreHoldsTwice()
    {
        var stores = Store(
            new DataPathConfiguration { Name = "dbo", Containers = [Container("Customers", Field("Id", 1, "Int32"))] },
            new DataPathConfiguration { Name = "archive", Containers = [Container("Customers", Field("ArchivedId", 1, "Int64"))] });

        var answer = await Over(stores).ContainerFields("src", "Crm", "ARCHIVE", "Customers", NullLogger.Instance, TestContext.Current.CancellationToken);

        answer.IsSuccess.ShouldBeTrue(answer.CurrentMessage);
        answer.Value.ShouldNotBeNull().Select(field => field.Name).ShouldBe(["ArchivedId"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5523")]
    public async Task ANameInTwoPathsWithNoPathStatedIsAFailureThatNamesBothPathsAndNoneIsPicked()
    {
        var stores = Store(
            new DataPathConfiguration { Name = "dbo", Containers = [Container("Customers", Field("Id", 1, "Int32"))] },
            new DataPathConfiguration { Name = "archive", Containers = [Container("Customers", Field("ArchivedId", 1, "Int64"))] });

        var answer = await Over(stores).ContainerFields("src", "Crm", null, "Customers", NullLogger.Instance, TestContext.Current.CancellationToken);

        answer.IsFailure.ShouldBeTrue();
        answer.Code!.Name.ShouldBe("ContainerFieldsAmbiguous");
        var message = answer.CurrentMessage.ShouldNotBeNull();
        message.ShouldContain("dbo");
        message.ShouldContain("archive");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5523")]
    public async Task AContainerTheStoreDoesNotHoldIsAFailureThatSaysSo()
    {
        var stores = Store(new DataPathConfiguration { Name = "dbo", Containers = [Container("Orders")] });

        var answer = await Over(stores).ContainerFields("src", "Crm", null, "Customers", NullLogger.Instance, TestContext.Current.CancellationToken);

        answer.IsFailure.ShouldBeTrue();
        answer.Code!.Name.ShouldBe("ContainerFieldsNotFound");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5523")]
    public async Task AContainerHeldOnlyInOtherPathsThanTheStatedOneIsAFailureThatNamesTheOnesItIsIn()
    {
        var stores = Store(new DataPathConfiguration { Name = "dbo", Containers = [Container("Customers")] });

        var answer = await Over(stores).ContainerFields("src", "Crm", "sales", "Customers", NullLogger.Instance, TestContext.Current.CancellationToken);

        answer.IsFailure.ShouldBeTrue();
        answer.Code!.Name.ShouldBe("ContainerFieldsInOtherPath");
        answer.CurrentMessage.ShouldNotBeNull().ShouldContain("dbo");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5523")]
    public async Task AStoreThatIsNotThereIsAFailureThatSaysTheStoreIsMissing()
    {
        var stores = new Mock<IDataStoreConfigurationProvider>();
        stores
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<ChildNarrowing>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStoreImplementationConfiguration>.Success(default!));

        var answer = await Over(stores).ContainerFields("src", "Crm", null, "Customers", NullLogger.Instance, TestContext.Current.CancellationToken);

        answer.IsFailure.ShouldBeTrue();
        answer.Code!.Name.ShouldBe("ContainerFieldsStoreNotFound");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5523")]
    public async Task AReadThatFailsForAnotherReasonIsTheAnswerWithItsOwnCode()
    {
        var stores = new Mock<IDataStoreConfigurationProvider>();
        stores
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<ChildNarrowing>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStoreImplementationConfiguration>.Failure(
                DataServiceResultCodes.ByName("GraphDataSetContainerNotFound"), NullLogger.Instance, ResultDetails.Create("component", "src", "dataSet", "Crm", "container", "Customers").With("dataStore", "Crm")));

        var answer = await Over(stores).ContainerFields("src", "Crm", null, "Customers", NullLogger.Instance, TestContext.Current.CancellationToken);

        answer.IsFailure.ShouldBeTrue();
        answer.Code!.Name.ShouldBe("GraphDataSetContainerNotFound");
    }
}
