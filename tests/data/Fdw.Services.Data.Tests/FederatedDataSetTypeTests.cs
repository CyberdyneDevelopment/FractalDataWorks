using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataContainers.Abstractions;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Execution;
using Fdw.Services.Data.Transformations;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;
using IDataField = Fdw.Data.Abstractions.IDataField;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// What a Federated data set joins on and answers with (FDW-3629).
/// </summary>
/// <remarks>
/// Why these exist: the strategy used a source's field mappings only to translate the filter, then joined and returned the records in
/// the store's names. A join on a logical name read null on every record and matched nothing, with no error. Each source here is a
/// mocked record-capable connection whose records carry the store's column names.
/// </remarks>
public sealed class FederatedDataSetTypeTests
{
    private static readonly Guid OrdersConnection = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid CustomersConnection = Guid.Parse("11111111-0000-0000-0000-000000000002");

    private sealed class Records(RecordSchema schema, IReadOnlyList<object?[]> rows) : IRecordSource<DataRecord>
    {
        public RecordSchema Schema => schema;

        public IEnumerable<IGenericResult<DataRecord>> Read() =>
            rows.Select(row => GenericResult<DataRecord>.Success(new DataRecord(schema, row)));

        public async IAsyncEnumerable<IGenericResult<DataRecord>> Read([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var row in rows)
            {
                await Task.Yield();
                yield return GenericResult<DataRecord>.Success(new DataRecord(schema, row));
            }
        }

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private static RecordSchema Schema(params string[] names) =>
        new([.. names.Select(name => Mock.Of<IDataField>(field => field.Name == name))]);

    private static DataSetSourceConfiguration Source(string name, string container, params (string Logical, string Physical)[] mappings) => new()
    {
        Name = name,
        SourceName = name,
        DataStoreName = "Store",
        PathValue = "dbo",
        ContainerName = container,
        Mappings = [.. mappings.Select(m => new DataSetFieldMappingConfiguration
        {
            Id = Guid.CreateVersion7(), Name = m.Logical, LogicalFieldName = m.Logical, PhysicalFieldName = m.Physical,
        })],
    };

    private static DataSetExecutionContext Context(
        IReadOnlyList<DataSetSourceConfiguration> sources, DataSetJoinConfiguration join, Records orders, Records customers)
    {
        var stores = new Mock<IDataStoreProvider>();
        Store(stores, "Order", OrdersConnection);
        Store(stores, "Customer", CustomersConnection);

        var connections = new Mock<IDataConnectionProvider>();
        Connection(connections, OrdersConnection, orders);
        Connection(connections, CustomersConnection, customers);

        // Every mapping has no transformation attached: the resolver reads the attachments and finds none.
        var gateway = new Mock<IConfigurationGateway>();
        gateway.Setup(g => g.Execute<IEnumerable<FieldMappingTransformationConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<FieldMappingTransformationConfiguration>>.Success([]));
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(g => g.Get(It.IsAny<string>())).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
        var resolver = new FieldMappingTransformationResolver(
            gateways.Object,
            new TransformationConfigurationProvider(
                NullLogger<TransformationConfigurationProvider>.Instance, gateways.Object, "PlatformConfiguration", "transform", "Transformation"));

        var config = new FederatedDataSetConfiguration
        {
            Name = "CustomerOrders",
            Implementation = "Federated",
            FederationStrategy = "Sequential",
            Sources = [.. sources],
            Joins = [join],
        };

        return new DataSetExecutionContext(
            config,
            connections.Object,
            stores.Object,
            new PredicatePushdownAnalyzer(NullLogger<PredicatePushdownAnalyzer>.Instance),
            resolver,
            NullLogger.Instance);
    }

    private static void Store(Mock<IDataStoreProvider> stores, string container, Guid connectionId)
    {
        var found = new Mock<IDataContainer> { DefaultValue = DefaultValue.Mock };
        found.Setup(c => c.Parent.Store.ConnectionId).Returns(connectionId);
        stores.Setup(s => s.Get("Store", "dbo", container, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataContainer>.Success(found.Object));
    }

    private static void Connection(Mock<IDataConnectionProvider> connections, Guid id, Records records)
    {
        var connection = new Mock<IDataConnection>();
        connection.As<IRecordSourceConnection>()
            .Setup(c => c.OpenRecordSource(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRecordSource<DataRecord>>.Success(records));
        connections.Setup(c => c.Get(id, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IDataConnection>.Success(connection.Object));
    }

    private static DataSetJoinConfiguration Join(string type, string left = "CustomerId", string right = "CustomerId") => new()
    {
        LeftSourceName = "Orders", LeftFieldName = left, RightSourceName = "Customers", RightFieldName = right, JoinType = type, Ordinal = 1,
    };

    private static IReadOnlyList<DataSetSourceConfiguration> MappedSources() =>
    [
        Source("Orders", "Order", ("CustomerId", "cust_id"), ("OrderTotal", "total")),
        Source("Customers", "Customer", ("CustomerId", "id"), ("CustomerName", "name")),
    ];

    private static Records Orders() => new(Schema("cust_id", "total"), [[7, 10m], [8, 25m]]);

    private static Records Customers() => new(Schema("id", "name"), [[7, "Acme"]]);

    private static Task<IGenericResult<IEnumerable<DataRecord>>> Run(DataSetExecutionContext context) =>
        new FederatedDataSetType().Execute<IEnumerable<DataRecord>>(context, new QueryCommand<DataRecord>(), TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Category", "DataSets.FDW3629")]
    public async Task SourcesWithDifferentPhysicalNamesJoinOnTheirLogicalNamesAndAnswerInThem()
    {
        var result = await Run(Context(MappedSources(), Join("Inner"), Orders(), Customers()));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var row = result.Value.ShouldNotBeNull().ShouldHaveSingleItem();
        row["CustomerName"].ShouldBe("Acme");
        row["OrderTotal"].ShouldBe(10m);
        row.Schema.GetFieldOrdinal("cust_id").ShouldBe(-1, "the result speaks the data set's names, not the store's");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW3629")]
    public async Task ALeftJoinKeepsTheLeftRowsThatMatchNothing()
    {
        var result = await Run(Context(MappedSources(), Join("Left"), Orders(), Customers()));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var rows = result.Value.ShouldNotBeNull().ToList();
        rows.Count.ShouldBe(2);
        rows.Single(r => Equals(r["OrderTotal"], 25m))["CustomerName"].ShouldBeNull();
    }

    [Fact]
    [Trait("Category", "DataSets.FDW3629")]
    public async Task AKeyBothSidesNameTheSameIsKeptOnceSoALeftRowThatMatchesNothingKeepsItsKey()
    {
        var result = await Run(Context(MappedSources(), Join("Left"), Orders(), Customers()));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var unmatched = result.Value.ShouldNotBeNull().Single(r => Equals(r["OrderTotal"], 25m));
        unmatched["CustomerId"].ShouldBe(8, "the right side's null must not shadow the left row's own key");
        unmatched.Schema.Fields.Count(f => string.Equals(f.Name, "CustomerId", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
    }

    [Fact]
    [Trait("Category", "DataSets.FDW3629")]
    public async Task AJoinOnAFieldItsSourceDoesNotHaveIsRefusedNotAnsweredWithNothing()
    {
        var result = await Run(Context(MappedSources(), Join("Inner", left: "Region"), Orders(), Customers()));

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Region");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW3629")]
    public async Task SourcesWithNoMappingsJoinOnTheirOwnNames()
    {
        var result = await Run(Context(
            [Source("Orders", "Order"), Source("Customers", "Customer")],
            Join("Inner", left: "cust_id", right: "id"),
            Orders(),
            Customers()));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldNotBeNull().ShouldHaveSingleItem()["name"].ShouldBe("Acme");
    }
}
