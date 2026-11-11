using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;
using IDataField = Fdw.Data.Abstractions.IDataField;

namespace Fdw.Services.Data.Tests.StableReads;

/// <summary>
/// Tests for the one place a gateway chooses how to stream rows in a stable order (FDW-4911): a connection's cursor, keyset pages by the
/// declared key, or a refusal; and for a data set, keyset pages, one unpaged read under the cap, or a refusal.
/// </summary>
public sealed class StableReadTests
{
    private static readonly Func<IDataCommand, CancellationToken, Task<IGenericResult<IEnumerable<Dictionary<string, object?>>>>> NoPages =
        (_, _) => throw new InvalidOperationException("no page should be read");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AKeylessContainerOnAConnectionThatCannotStreamIsRefusedWithItsCode()
    {
        var container = Container(keys: []);

        var opened = await StableRead.OpenContainer(
            new QueryCommand<Dictionary<string, object?>>(), container, new Mock<IDataConnection>().Object, NoPages, "container 'Orders'", NullLogger.Instance,
            TestContext.Current.CancellationToken);

        opened.IsFailure.ShouldBeTrue();
        var message = opened.CurrentMessage.ShouldNotBeNull();
        message.ShouldContain("'container 'Orders'' cannot be read in a stable order");
        message.ShouldContain("declares no primary key");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AConnectionThatCanStreamIsReadThroughItsCursorCutToTheFieldsTheQueryReturns()
    {
        var container = Container(keys: ["Id"]);
        var cursor = new RowListRecordSource(
            new RecordSchema([Field("Id"), Field("Name"), Field("RowVersion")]),
            [new Dictionary<string, object?> { ["Id"] = 1, ["Name"] = "a", ["RowVersion"] = null }]);
        var connection = new Mock<IDataConnection>();
        connection.As<IRecordSourceConnection>()
            .Setup(c => c.OpenRecordSource(It.IsAny<IDataCommand>(), container, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRecordSource<DataRecord>>.Success(cursor));

        var opened = await StableRead.OpenContainer(
            new QueryCommand<Dictionary<string, object?>>(), container, connection.Object, NoPages, "container 'Orders'", NullLogger.Instance,
            TestContext.Current.CancellationToken);

        opened.IsSuccess.ShouldBeTrue(opened.CurrentMessage);
        var source = opened.Value.ShouldNotBeNull();
        source.Schema.Fields.Select(f => f.Name).ShouldBe(["Id", "Name"]);
        var record = source.Read().Single();
        record.Value["Name"].ShouldBe("a");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AKeyedContainerOnAConnectionThatCannotStreamIsReadInKeysetPagesOfTheConfiguredSize()
    {
        var container = Container(keys: ["Region", "Id"]);
        var asked = new List<IQueryCommand>();
        Task<IGenericResult<IEnumerable<Dictionary<string, object?>>>> Page(IDataCommand command, CancellationToken _)
        {
            asked.Add((IQueryCommand)command);
            return Task.FromResult<IGenericResult<IEnumerable<Dictionary<string, object?>>>>(
                GenericResult<IEnumerable<Dictionary<string, object?>>>.Success([new Dictionary<string, object?> { ["Region"] = 1, ["Id"] = 1, ["Name"] = "a" }]));
        }

        var opened = await StableRead.OpenContainer(
            new QueryCommand<Dictionary<string, object?>>(), container, new Mock<IDataConnection>().Object, Page, "container 'Orders'", NullLogger.Instance,
            TestContext.Current.CancellationToken);

        opened.IsSuccess.ShouldBeTrue(opened.CurrentMessage);
        var source = opened.Value.ShouldBeOfType<KeysetRecordSource>();
        source.Read().Count().ShouldBe(1);
        var first = asked.Single();
        first.Ordering.ShouldNotBeNull().OrderedFields.Select(f => f.PropertyName).ShouldBe(["Region", "Id"]);
        first.Paging.ShouldNotBeNull().Take.ShouldBe(DataGatewayServiceTypes.StableReadPageSize);
        first.Paging!.Skip.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AKeylessContainerWhoseStoreKeepsItsOwnOrderIsPagedInThatOrder()
    {
        var container = Container(keys: []);
        var connection = new Mock<IDataConnection>();
        connection.As<IStoreOrderedConnection>().Setup(c => c.StoreOrder).Returns("the order the remote API returns");
        var asked = new List<IQueryCommand>();
        Task<IGenericResult<IEnumerable<Dictionary<string, object?>>>> Page(IDataCommand command, CancellationToken _)
        {
            asked.Add((IQueryCommand)command);
            return Task.FromResult<IGenericResult<IEnumerable<Dictionary<string, object?>>>>(
                GenericResult<IEnumerable<Dictionary<string, object?>>>.Success([new Dictionary<string, object?> { ["Region"] = 1, ["Id"] = 1, ["Name"] = "a" }]));
        }

        var opened = await StableRead.OpenContainer(
            new QueryCommand<Dictionary<string, object?>>(), container, connection.Object, Page, "container 'Orders'", NullLogger.Instance,
            TestContext.Current.CancellationToken);

        opened.IsSuccess.ShouldBeTrue(opened.CurrentMessage);
        var source = opened.Value.ShouldBeOfType<StoreOrderedRecordSource>();
        source.Read().Count().ShouldBe(1);
        var first = asked.Single();
        first.Ordering.ShouldBeNull();
        first.Paging.ShouldNotBeNull().Skip.ShouldBe(0);
        first.Paging!.Take.ShouldBe(DataGatewayServiceTypes.StableReadPageSize);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AWriteIsNotAStableReadAndIsRefusedWithItsCode()
    {
        var command = new Mock<IDataCommand>().Object;

        var opened = await StableRead.OpenContainer(
            command, Container(keys: ["Id"]), new Mock<IDataConnection>().Object, NoPages, "container 'Orders'", NullLogger.Instance,
            TestContext.Current.CancellationToken);

        opened.IsFailure.ShouldBeTrue();
        opened.CurrentMessage.ShouldNotBeNull().ShouldContain("takes a query command");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ADataSetThatPagesInTheStoreAndDeclaresNoKeyIsRefused()
    {
        var dataSet = new SimpleDataSetConfiguration { Name = "Teams", Implementation = "Simple", Fields = [new DataSetFieldConfiguration { Name = "Name" }] };

        var opened = await StableRead.OpenDataSet(new QueryCommand<Dictionary<string, object?>>(), dataSet, NoPages, null, NullLogger.Instance, TestContext.Current.CancellationToken);

        opened.IsFailure.ShouldBeTrue();
        opened.CurrentMessage.ShouldNotBeNull().ShouldContain("declares none");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ADataSetThatPagesInTheStoreIsReadInKeysetPagesByItsKeyFields()
    {
        var dataSet = new SimpleDataSetConfiguration
        {
            Name = "Teams",
            Implementation = "Simple",
            Fields = [new DataSetFieldConfiguration { Name = "Id", Ordinal = 0, IsKey = true }, new DataSetFieldConfiguration { Name = "Name", Ordinal = 1 }],
        };
        var store = new ShufflingStore([.. Enumerable.Range(0, 7).Select(i => new Dictionary<string, object?> { ["Id"] = i, ["Name"] = $"t{i}" })]);

        var opened = await StableRead.OpenDataSet(new QueryCommand<Dictionary<string, object?>>(), dataSet, store.Read, null, NullLogger.Instance, TestContext.Current.CancellationToken);

        opened.IsSuccess.ShouldBeTrue(opened.CurrentMessage);
        var source = opened.Value.ShouldBeOfType<KeysetRecordSource>();
        source.Schema.Fields.Select(f => f.Name).ShouldBe(["Id", "Name"]);
        source.Read().Select(r => (int)r.Value["Id"]!).ShouldBe(Enumerable.Range(0, 7));
        store.Orderings.ShouldAllBe(order => order == "Id");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnInMemoryJoinIsReadOnceUnpaged()
    {
        var store = new ShufflingStore([.. Enumerable.Range(0, 1500).Select(i => new Dictionary<string, object?> { ["Id"] = i })], compare: false);

        var opened = await StableRead.OpenDataSet(new QueryCommand<Dictionary<string, object?>>(), Federated(), store.Read, null, NullLogger.Instance, TestContext.Current.CancellationToken);

        opened.IsSuccess.ShouldBeTrue(opened.CurrentMessage);
        opened.Value.ShouldNotBeNull().Read().Count().ShouldBe(1500);
        store.Reads.ShouldBe(1);
        ((IQueryCommand)store.Commands.Single()).Paging.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnInMemoryJoinOverTheConfiguredCapIsRefusedWithItsCode()
    {
        var cap = DataGatewayServiceTypes.UnpagedDataSetRowCap;
        var store = new ShufflingStore([.. Enumerable.Range(0, cap + 1).Select(i => new Dictionary<string, object?> { ["Id"] = i })], compare: false);

        var opened = await StableRead.OpenDataSet(new QueryCommand<Dictionary<string, object?>>(), Federated(), store.Read, null, NullLogger.Instance, TestContext.Current.CancellationToken);

        opened.IsFailure.ShouldBeTrue();
        opened.CurrentMessage.ShouldNotBeNull().ShouldContain($"more than the {cap}");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AKeylessDataSetWhoseConnectionStreamsIsReadThroughTheStreamNotRefused()
    {
        var dataSet = new SimpleDataSetConfiguration { Name = "Teams", Implementation = "Simple", Fields = [new DataSetFieldConfiguration { Name = "Name" }] };
        var stream = new RowListRecordSource(new RecordSchema([Field("Name")]), [new Dictionary<string, object?> { ["Name"] = "a" }]);
        var askedBatch = 0;
        Task<IGenericResult<IRecordSource<DataRecord>?>> Stream(int batchSize, CancellationToken _)
        {
            askedBatch = batchSize;
            return Task.FromResult<IGenericResult<IRecordSource<DataRecord>?>>(GenericResult<IRecordSource<DataRecord>?>.Success(stream));
        }

        var opened = await StableRead.OpenDataSet(new QueryCommand<Dictionary<string, object?>>(), dataSet, NoPages, Stream, NullLogger.Instance, TestContext.Current.CancellationToken);

        opened.IsSuccess.ShouldBeTrue(opened.CurrentMessage);
        opened.Value.ShouldBeSameAs(stream);
        askedBatch.ShouldBe(DataGatewayServiceTypes.StableReadPageSize);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AKeylessDataSetWhoseConnectionCannotStreamIsRefusedSayingSo()
    {
        var dataSet = new SimpleDataSetConfiguration { Name = "Teams", Implementation = "Simple", Fields = [new DataSetFieldConfiguration { Name = "Name" }] };

        var opened = await StableRead.OpenDataSet(
            new QueryCommand<Dictionary<string, object?>>(), dataSet, NoPages, NoStream, NullLogger.Instance, TestContext.Current.CancellationToken);

        opened.IsFailure.ShouldBeTrue();
        opened.CurrentMessage.ShouldNotBeNull().ShouldContain("its connection cannot stream it");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AKeyedDataSetWhoseConnectionCannotStreamIsReadInKeysetPages()
    {
        var dataSet = new SimpleDataSetConfiguration
        {
            Name = "Teams",
            Implementation = "Simple",
            Fields = [new DataSetFieldConfiguration { Name = "Id", Ordinal = 0, IsKey = true }],
        };
        var store = new ShufflingStore([.. Enumerable.Range(0, 3).Select(i => new Dictionary<string, object?> { ["Id"] = i })]);

        var opened = await StableRead.OpenDataSet(
            new QueryCommand<Dictionary<string, object?>>(), dataSet, store.Read, NoStream, NullLogger.Instance, TestContext.Current.CancellationToken);

        opened.IsSuccess.ShouldBeTrue(opened.CurrentMessage);
        opened.Value.ShouldBeOfType<KeysetRecordSource>().Read().Count().ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStreamThatFailsToOpenFailsTheReadAndIsNotPagedInstead()
    {
        var dataSet = new SimpleDataSetConfiguration
        {
            Name = "Teams",
            Implementation = "Simple",
            Fields = [new DataSetFieldConfiguration { Name = "Id", Ordinal = 0, IsKey = true }],
        };
        static Task<IGenericResult<IRecordSource<DataRecord>?>> Broken(int batchSize, CancellationToken _) =>
            Task.FromResult<IGenericResult<IRecordSource<DataRecord>?>>(
                GenericResult<IRecordSource<DataRecord>?>.Failure(new Fdw.Messages.GenericMessage("cursor refused")));

        var opened = await StableRead.OpenDataSet(
            new QueryCommand<Dictionary<string, object?>>(), dataSet, NoPages, Broken, NullLogger.Instance, TestContext.Current.CancellationToken);

        opened.IsFailure.ShouldBeTrue();
        opened.CurrentMessage.ShouldNotBeNull().ShouldContain("cursor refused");
    }

    private static Task<IGenericResult<IRecordSource<DataRecord>?>> NoStream(int batchSize, CancellationToken cancellationToken) =>
        Task.FromResult<IGenericResult<IRecordSource<DataRecord>?>>(GenericResult<IRecordSource<DataRecord>?>.Success(null));

    private static FederatedDataSetConfiguration Federated() => new()
    {
        Name = "BreweriesInNflCities",
        Implementation = "Federated",
        Fields = [new DataSetFieldConfiguration { Name = "Id", Ordinal = 0, IsKey = true }],
    };

    // A container with the fields Region, Id and Name, all returned, and a primary key of the fields named.
    private static IDataContainer Container(IReadOnlyList<string> keys)
    {
        var fields = new[] { SchemaField("Region"), SchemaField("Id"), SchemaField("Name") };
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(fields);
        schema.Setup(s => s.GetReturnedFields(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns(fields);

        var containerKeys = new List<IContainerKey>();
        if (keys.Count > 0)
        {
            var key = new Mock<IContainerKey>();
            key.Setup(k => k.KeyType).Returns(new PrimaryKeyType());
            key.Setup(k => k.KeyFields).Returns(keys.Select(name => Mock.Of<IContainerKeyField>(field => field.LocalField == Field(name))).ToList());
            containerKeys.Add(key.Object);
        }

        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns("Orders");
        container.As<IStorageContainer>().Setup(c => c.Schema).Returns(schema.Object);
        container.Setup(c => c.Keys).Returns(containerKeys);
        return container.Object;
    }

    private static IField SchemaField(string name)
    {
        var field = new Mock<IField>();
        field.Setup(f => f.Name).Returns(name);
        field.As<IDataField>().Setup(f => f.Name).Returns(name);
        return field.Object;
    }

    private static IDataField Field(string name) => Mock.Of<IDataField>(field => field.Name == name);
}
