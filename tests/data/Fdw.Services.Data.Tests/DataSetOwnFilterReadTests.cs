using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Fdw.Services.Data.Tests;

/// <summary>
/// A data set's own filter goes to the store with every read of it, ANDed with the caller's (FDW-4997): a Simple page, a Simple stream, and
/// a Federated read, each in its source's names. Compound's is in <see cref="CompoundDataSetPagingTests"/>, whose store evaluates it.
/// </summary>
public sealed class DataSetOwnFilterReadTests
{
    private static readonly Guid ConnectionId = Guid.Parse("33333333-0000-0000-0000-000000000001");

    private static DataSetFilterConditionConfiguration OpenOnly() =>
        new() { FieldName = "Status", Operator = "Equal", Value = "open", DataType = "String", Ordinal = 0 };

    private static FilterExpression CallerFilter() => new()
    {
        Root = new FilterCondition { PropertyName = "Quantity", Operator = FilterOperators.ByName("GreaterThan"), Value = 3 },
    };

    // A Simple data set over one source that maps Status to status and Quantity to qty, whose connection records every command and can stream.
    private static (DataSetExecutionContext Context, List<IDataCommand> Sent) Simple(params DataSetFilterConditionConfiguration[] filters)
    {
        var container = new Mock<IDataContainer> { DefaultValue = DefaultValue.Mock };
        container.Setup(c => c.Parent.Store.ConnectionId).Returns(ConnectionId);
        var stores = new Mock<IDataStoreProvider>();
        stores.Setup(s => s.Get("Store", "dbo", "Order", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataContainer>.Success(container.Object));

        var sent = new List<IDataCommand>();
        var connection = new Mock<IDataConnection>();
        connection.Setup(c => c.Execute<IEnumerable<IDataRow>>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, IDataContainer, CancellationToken>((command, _, _) => sent.Add(command))
            .ReturnsAsync(GenericResult<IEnumerable<IDataRow>>.Success(Array.Empty<IDataRow>()));
        connection.As<IRecordSourceConnection>()
            .Setup(c => c.OpenRecordSource(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, IDataContainer, CancellationToken>((command, _, _) => sent.Add(command))
            .ReturnsAsync(GenericResult<IRecordSource<DataRecord>>.Success(new Mock<IRecordSource<DataRecord>>().Object));
        var connections = new Mock<IDataConnectionProvider>();
        connections.Setup(c => c.Get(ConnectionId, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IDataConnection>.Success(connection.Object));

        var config = new SimpleDataSetConfiguration
        {
            Name = "Orders",
            Implementation = "Simple",
            Fields =
            [
                new DataSetFieldConfiguration { Name = "OrderId", Ordinal = 0, IsKey = true },
                new DataSetFieldConfiguration { Name = "Status", Ordinal = 1 },
                new DataSetFieldConfiguration { Name = "Quantity", Ordinal = 2 },
            ],
            Sources =
            [
                new DataSetSourceConfiguration
                {
                    Name = "Orders", SourceName = "Orders", DataStoreName = "Store", PathValue = "dbo", ContainerName = "Order",
                    Mappings =
                    [
                        new DataSetFieldMappingConfiguration { Id = Guid.CreateVersion7(), Name = "OrderId", LogicalFieldName = "OrderId", PhysicalFieldName = "order_id" },
                        new DataSetFieldMappingConfiguration { Id = Guid.CreateVersion7(), Name = "Status", LogicalFieldName = "Status", PhysicalFieldName = "status" },
                        new DataSetFieldMappingConfiguration { Id = Guid.CreateVersion7(), Name = "Quantity", LogicalFieldName = "Quantity", PhysicalFieldName = "qty" },
                    ],
                },
            ],
            Filters = [.. filters],
        };

        return (new DataSetExecutionContext(
            config, connections.Object, stores.Object,
            new PredicatePushdownAnalyzer(NullLogger<PredicatePushdownAnalyzer>.Instance), Resolver(), NullLogger.Instance), sent);
    }

    private static FieldMappingTransformationResolver Resolver()
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway.Setup(g => g.Execute<IEnumerable<FieldMappingTransformationConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<FieldMappingTransformationConfiguration>>.Success([]));
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(g => g.Get(It.IsAny<string>())).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
        return new FieldMappingTransformationResolver(
            gateways.Object,
            new TransformationConfigurationProvider(
                NullLogger<TransformationConfigurationProvider>.Instance, gateways.Object, "PlatformConfiguration", "transform", "Transformation"));
    }

    private static List<string> ConditionNames(IDataCommand sent) =>
        ((IQueryCommand)sent).Filter.ShouldNotBeNull().Root switch
        {
            FilterGroup group => [.. group.Nodes.Cast<IFilterCondition>().Select(c => c.PropertyName)],
            IFilterCondition condition => [condition.PropertyName],
            _ => [],
        };

    [Fact]
    [Trait("Category", "DataSets.FDW4997")]
    public async Task ASimplePageCarriesTheDataSetsFilterAndTheCallersInTheSourcesNames()
    {
        var (context, sent) = Simple(OpenOnly());

        var result = await new SimpleDataSetType().Execute<IEnumerable<IDataRow>>(
            context,
            new QueryCommand<IDataRow> { Filter = CallerFilter(), Paging = new PagingExpression { Skip = 0, Take = 10 } },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        ConditionNames(sent.ShouldHaveSingleItem()).ShouldBe(["status", "qty"]);
    }

    [Fact]
    [Trait("Category", "DataSets.FDW4997")]
    public async Task ASimpleReadWithNoCallerFilterStillCarriesTheDataSetsFilter()
    {
        var (context, sent) = Simple(OpenOnly());

        var result = await new SimpleDataSetType().Execute<IEnumerable<IDataRow>>(
            context, new QueryCommand<IDataRow>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var condition = ((IQueryCommand)sent.ShouldHaveSingleItem()).Filter.ShouldNotBeNull().Root.ShouldBeAssignableTo<IFilterCondition>().ShouldNotBeNull();
        condition.PropertyName.ShouldBe("status");
        condition.Value.ShouldBe("open");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW4997")]
    public async Task ASimpleStreamCarriesTheDataSetsFilterAndTheCallers()
    {
        var (context, sent) = Simple(OpenOnly());

        var opened = await new SimpleDataSetType().OpenStream(
            context, new QueryCommand<IDataRow> { Filter = CallerFilter() }, 100, TestContext.Current.CancellationToken);

        opened.IsSuccess.ShouldBeTrue(opened.CurrentMessage);
        ConditionNames(sent.ShouldHaveSingleItem()).ShouldBe(["status", "qty"]);
    }

    [Fact]
    [Trait("Category", "DataSets.FDW4997")]
    public async Task AStoredConditionThatBreaksARuleIsRefusedAtReadAndTheStoreIsNotAsked()
    {
        var (context, sent) = Simple(new DataSetFilterConditionConfiguration { FieldName = "Status", Operator = "Equals", Value = "open", DataType = "String" });

        var result = await new SimpleDataSetType().Execute<IEnumerable<IDataRow>>(
            context, new QueryCommand<IDataRow>(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code!.Code.ShouldBe("DATASETS-20023");
        sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Category", "DataSets.FDW4997")]
    public async Task AFederatedReadSendsEachSourceItsShareOfTheDataSetsFilter()
    {
        var stores = new Mock<IDataStoreProvider>();
        var connections = new Mock<IDataConnectionProvider>();
        var sentBySource = new Dictionary<string, IDataCommand>(StringComparer.Ordinal);
        foreach (var (container, id, schema) in new[]
        {
            ("Order", Guid.Parse("33333333-0000-0000-0000-000000000002"), new[] { "cust_id", "status" }),
            ("Customer", Guid.Parse("33333333-0000-0000-0000-000000000003"), new[] { "id", "name" }),
        })
        {
            var found = new Mock<IDataContainer> { DefaultValue = DefaultValue.Mock };
            found.Setup(c => c.Parent.Store.ConnectionId).Returns(id);
            stores.Setup(s => s.Get("Store", "dbo", container, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IDataContainer>.Success(found.Object));

            var records = new Mock<IRecordSource<DataRecord>>();
            records.Setup(r => r.Schema).Returns(new RecordSchema([.. schema.Select(name => Mock.Of<Fdw.Data.Abstractions.IDataField>(f => f.Name == name))]));
            records.Setup(r => r.Read(It.IsAny<CancellationToken>())).Returns(NoRows());
            var connection = new Mock<IDataConnection>();
            connection.As<IRecordSourceConnection>()
                .Setup(c => c.OpenRecordSource(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
                .Callback<IDataCommand, IDataContainer, CancellationToken>((command, _, _) => sentBySource[container] = command)
                .ReturnsAsync(GenericResult<IRecordSource<DataRecord>>.Success(records.Object));
            connections.Setup(c => c.Get(id, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IDataConnection>.Success(connection.Object));
        }

        var config = new FederatedDataSetConfiguration
        {
            Name = "CustomerOrders",
            Implementation = "Federated",
            FederationStrategy = "Sequential",
            Fields =
            [
                new DataSetFieldConfiguration { Name = "CustomerId", Ordinal = 0 },
                new DataSetFieldConfiguration { Name = "Status", Ordinal = 1 },
                new DataSetFieldConfiguration { Name = "CustomerName", Ordinal = 2 },
            ],
            Sources =
            [
                Source("Orders", "Order", ("CustomerId", "cust_id"), ("Status", "status")),
                Source("Customers", "Customer", ("CustomerId", "id"), ("CustomerName", "name")),
            ],
            Joins = [new DataSetJoinConfiguration { LeftSourceName = "Orders", LeftFieldName = "CustomerId", RightSourceName = "Customers", RightFieldName = "CustomerId", JoinType = "Inner", Ordinal = 1 }],
            Filters = [OpenOnly()],
        };
        var context = new DataSetExecutionContext(
            config, connections.Object, stores.Object,
            new PredicatePushdownAnalyzer(NullLogger<PredicatePushdownAnalyzer>.Instance), Resolver(), NullLogger.Instance);

        var result = await new FederatedDataSetType().Execute<IEnumerable<DataRecord>>(
            context, new QueryCommand<DataRecord>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var orders = ((IQueryCommand)sentBySource["Order"]).Filter.ShouldNotBeNull().Root.ShouldBeAssignableTo<IFilterCondition>().ShouldNotBeNull();
        orders.PropertyName.ShouldBe("status");
        orders.Value.ShouldBe("open");
        ((IQueryCommand)sentBySource["Customer"]).Filter.ShouldBeNull();
    }

    private static async IAsyncEnumerable<IGenericResult<DataRecord>> NoRows()
    {
        await Task.CompletedTask;
        yield break;
    }

    private static DataSetSourceConfiguration Source(string name, string container, params (string Logical, string Physical)[] mappings) => new()
    {
        Name = name,
        SourceName = name,
        DataStoreName = "Store",
        PathValue = "dbo",
        ContainerName = container,
        // Federated sends a source its share of a filter only when the source says its store applies one (PredicatePushdownAnalyzer).
        SupportsPredicatePushdown = true,
        Mappings = [.. mappings.Select(m => new DataSetFieldMappingConfiguration
        {
            Id = Guid.CreateVersion7(), Name = m.Logical, LogicalFieldName = m.Logical, PhysicalFieldName = m.Physical,
        })],
    };
}
