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

    // Reads a Compound data set joining Orders to Customers by the given join type; returns the result and the query the store was sent, if any.
    private static async Task<(IGenericResult<IEnumerable<IDataRow>> Result, IQueryCommand? Sent)> Read(string joinType)
    {
        var container = new Mock<IDataContainer> { DefaultValue = DefaultValue.Mock };
        container.Setup(c => c.Parent.Store.ConnectionId).Returns(ConnectionId);
        var stores = new Mock<IDataStoreProvider>();
        stores.Setup(s => s.Get("Store", "dbo", "Order", It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
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
            Joins = [new DataSetJoinConfiguration { LeftSourceName = "Orders", LeftFieldName = "CustomerId", RightSourceName = "Customers", RightFieldName = "Id", JoinType = joinType, Ordinal = 1 }],
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
        return (result, received as IQueryCommand);
    }

    [Fact]
    [Trait("Category", "DataSets.FDW3629")]
    public async Task TheStoreIsAskedToJoinOnPhysicalNamesAndAliasEveryColumnToItsLogicalName()
    {
        var (result, received) = await Read("Inner");

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var sent = received.ShouldNotBeNull();
        var join = sent.Joins.ShouldHaveSingleItem();
        join.TargetContainerName.ShouldBe("Customer");
        join.JoinConditions.ShouldHaveSingleItem().ShouldBe(("cust_id", "id"));
        var projected = sent.Projection.ShouldNotBeNull().Fields;
        projected.Single(f => f.Alias == "OrderTotal").PropertyName.ShouldBe("total");
        projected.Single(f => f.Alias == "CustomerName").PropertyName.ShouldBe("name");
    }

    [Theory]
    [Trait("Category", "DataSets.FDW5099")]
    [InlineData("inner", "Inner")]
    [InlineData("LEFT", "Left")]
    [InlineData("Full", "Full")]
    public async Task TheStoreIsSentTheJoinTypesOptionByItsOwnNameNotTheStoredText(string stored, string sentName)
    {
        var (result, received) = await Read(stored);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var join = received.ShouldNotBeNull().Joins.ShouldHaveSingleItem();
        join.JoinType.ShouldBe(sentName);
        join.JoinConditions.ShouldHaveSingleItem().ShouldBe(("cust_id", "id"));
    }

    [Fact]
    [Trait("Category", "DataSets.FDW5099")]
    public async Task ACrossJoinIsSentWithNoJoinColumns()
    {
        var (result, received) = await Read("Cross");

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var join = received.ShouldNotBeNull().Joins.ShouldHaveSingleItem();
        join.JoinType.ShouldBe("Cross");
        join.JoinConditions.ShouldBeEmpty();
    }

    [Theory]
    [Trait("Category", "DataSets.FDW5099")]
    [InlineData("None", "DATASETS-20028")]
    [InlineData("Inner JOIN x ON 1=1 --", "DATASETS-20029")]
    [InlineData("", "DATASETS-20030")]
    public async Task AJoinTypeThatDoesNotJoinIsRefusedBeforeTheStoreIsAskedAnything(string joinType, string code)
    {
        var (result, received) = await Read(joinType);

        result.IsFailure.ShouldBeTrue();
        result.Code!.Code.ShouldBe(code);
        received.ShouldBeNull("nothing is sent to the store");
    }
}
