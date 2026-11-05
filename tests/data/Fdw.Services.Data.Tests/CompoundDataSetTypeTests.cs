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
/// What a Compound data set asks its one store (FDW-3629): the join in the store's names, and every column aliased to the data set's name.
/// </summary>
/// <remarks>Why these exist: there was no Compound execution test. The source is a mocked connection that records the query it is sent.</remarks>
public sealed class CompoundDataSetTypeTests
{
    private static readonly Guid ConnectionId = Guid.Parse("22222222-0000-0000-0000-000000000001");

    private static DataSetSourceConfiguration Source(string name, string container, int priority, params (string Logical, string Physical)[] mappings) => new()
    {
        Name = name,
        SourceName = name,
        DataStoreName = "Store",
        PathValue = "dbo",
        ContainerName = container,
        Priority = priority,
        Mappings = [.. mappings.Select(m => new DataSetFieldMappingConfiguration
        {
            Id = Guid.CreateVersion7(), Name = m.Logical, LogicalFieldName = m.Logical, PhysicalFieldName = m.Physical,
        })],
    };

    [Fact]
    [Trait("Category", "DataSets.FDW3629")]
    public async Task TheStoreIsAskedToJoinOnPhysicalNamesAndAliasEveryColumnToItsLogicalName()
    {
        var container = new Mock<IDataContainer> { DefaultValue = DefaultValue.Mock };
        container.Setup(c => c.Parent.Store.ConnectionId).Returns(ConnectionId);
        var stores = new Mock<IDataStoreProvider>();
        stores.Setup(s => s.Get("Store", "dbo", "Order", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataContainer>.Success(container.Object));

        IDataCommand? received = null;
        var connection = new Mock<IDataConnection>();
        connection.Setup(c => c.Execute<IEnumerable<IDataRow>>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, IDataContainer, CancellationToken>((command, _, _) => received = command)
            .ReturnsAsync(GenericResult<IEnumerable<IDataRow>>.Success(Array.Empty<IDataRow>()));
        var connections = new Mock<IDataConnectionProvider>();
        connections.Setup(c => c.Get(ConnectionId, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IDataConnection>.Success(connection.Object));

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

        var config = new CompoundDataSetConfiguration
        {
            Name = "CustomerOrders",
            Implementation = "Compound",
            Sources =
            [
                Source("Orders", "Order", 1, ("CustomerId", "cust_id"), ("OrderTotal", "total")),
                Source("Customers", "Customer", 2, ("CustomerName", "name"), ("Id", "id")),
            ],
            Joins = [new DataSetJoinConfiguration { LeftSourceName = "Orders", LeftFieldName = "CustomerId", RightSourceName = "Customers", RightFieldName = "Id", JoinType = "Inner", Ordinal = 1 }],
            Fields =
            [
                new DataSetFieldConfiguration { Name = "OrderTotal" },
                new DataSetFieldConfiguration { Name = "CustomerName" },
            ],
        };
        var context = new DataSetExecutionContext(
            config, connections.Object, stores.Object,
            new PredicatePushdownAnalyzer(NullLogger<PredicatePushdownAnalyzer>.Instance), resolver, NullLogger.Instance);

        var result = await new CompoundDataSetType()
            .Execute<IEnumerable<IDataRow>>(context, new QueryCommand<IDataRow>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var sent = received.ShouldBeAssignableTo<IQueryCommand>().ShouldNotBeNull();
        var join = sent.Joins.ShouldHaveSingleItem();
        join.TargetContainerName.ShouldBe("Customer");
        join.JoinConditions.ShouldHaveSingleItem().ShouldBe(("cust_id", "id"));
        var projected = sent.Projection.ShouldNotBeNull().Fields;
        projected.Single(f => f.Alias == "OrderTotal").PropertyName.ShouldBe("total");
        projected.Single(f => f.Alias == "CustomerName").PropertyName.ShouldBe("name");
    }
}
