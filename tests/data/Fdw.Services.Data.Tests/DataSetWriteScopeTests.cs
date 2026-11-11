using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
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
/// A write through a Simple data set with its own filters touches only the rows they keep (FDW-5069): an update or delete is narrowed to
/// them in the source's names, an insert row outside them is refused, and a write that cannot be narrowed is refused.
/// </summary>
public sealed class DataSetWriteScopeTests
{
    private static readonly Guid ConnectionId = Guid.Parse("44444444-0000-0000-0000-000000000001");

    private static DataSetFilterConditionConfiguration OpenOnly() =>
        new() { FieldName = "Status", Operator = "Equal", Value = "open", DataType = "String", Ordinal = 0 };

    private static (DataSetExecutionContext Context, List<IDataCommand> Sent) Orders(params DataSetFilterConditionConfiguration[] filters)
    {
        var container = new Mock<IDataContainer> { DefaultValue = DefaultValue.Mock };
        container.Setup(c => c.Parent.Store.ConnectionId).Returns(ConnectionId);
        var stores = new Mock<IDataStoreProvider>();
        stores.Setup(s => s.Get("Store", "dbo", "Order", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataContainer>.Success(container.Object));

        var sent = new List<IDataCommand>();
        var connection = new Mock<IDataConnection>();
        connection.Setup(c => c.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, IDataContainer, CancellationToken>((command, _, _) => sent.Add(command))
            .ReturnsAsync(GenericResult<int>.Success(1));
        var connections = new Mock<IDataConnectionProvider>();
        connections.Setup(c => c.Get(ConnectionId, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IDataConnection>.Success(connection.Object));

        var gateways = new Mock<IConfigurationGatewayProvider>();
        var resolver = new FieldMappingTransformationResolver(
            gateways.Object,
            new TransformationConfigurationProvider(
                NullLogger<TransformationConfigurationProvider>.Instance, gateways.Object, "PlatformConfiguration", "transform", "Transformation"));

        var config = new SimpleDataSetConfiguration
        {
            Name = "OpenOrders",
            Implementation = "Simple",
            Fields = [new DataSetFieldConfiguration { Name = "Status", Ordinal = 0 }, new DataSetFieldConfiguration { Name = "Quantity", Ordinal = 1 }],
            Sources =
            [
                new DataSetSourceConfiguration
                {
                    Name = "Orders", SourceName = "Orders", DataStoreName = "Store", PathValue = "dbo", ContainerName = "Order",
                    Mappings =
                    [
                        new DataSetFieldMappingConfiguration { Id = Guid.CreateVersion7(), Name = "Status", LogicalFieldName = "Status", PhysicalFieldName = "status" },
                        new DataSetFieldMappingConfiguration { Id = Guid.CreateVersion7(), Name = "Quantity", LogicalFieldName = "Quantity", PhysicalFieldName = "qty" },
                    ],
                },
            ],
            Filters = [.. filters],
        };

        return (new DataSetExecutionContext(
            config, connections.Object, stores.Object,
            new PredicatePushdownAnalyzer(NullLogger<PredicatePushdownAnalyzer>.Instance), resolver, NullLogger.Instance), sent);
    }

    private static Task<IGenericResult<int>> Write(DataSetExecutionContext context, IDataCommand command) =>
        new SimpleDataSetType().Execute<int>(context, command, TestContext.Current.CancellationToken);

    private static Dictionary<string, object?> Row(string status) => new(StringComparer.Ordinal) { ["status"] = status, ["qty"] = 4 };

    [Fact]
    [Trait("Category", "DataSets.FDW5069")]
    public async Task AnUpdateThroughAFilteredDataSetIsNarrowedToItsFiltersInTheSourcesNames()
    {
        var (context, sent) = Orders(OpenOnly());
        var update = new UpdateCommand<Dictionary<string, object?>>(Row("open"))
        {
            Filter = new FilterExpression { Root = new FilterCondition { PropertyName = "qty", Operator = FilterOperators.ByName("GreaterThan"), Value = 3 } },
        };

        var result = await Write(context, update);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var group = ((IFilterableCommand)sent.ShouldHaveSingleItem()).Filter.ShouldNotBeNull().Root.ShouldBeOfType<FilterGroup>();
        group.Nodes.Cast<IFilterCondition>().Select(c => c.PropertyName).ShouldBe(["status", "qty"]);
    }

    [Fact]
    [Trait("Category", "DataSets.FDW5069")]
    public async Task ADeleteOfEveryRowThroughAFilteredDataSetDeletesOnlyItsRows()
    {
        var (context, sent) = Orders(OpenOnly());

        var result = await Write(context, new DeleteCommand { AllRows = true });

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var delete = sent.ShouldHaveSingleItem().ShouldBeOfType<DeleteCommand>();
        delete.AllRows.ShouldBeFalse();
        delete.Filter.ShouldNotBeNull().Root.ShouldBeAssignableTo<IFilterCondition>().ShouldNotBeNull().PropertyName.ShouldBe("status");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW5069")]
    public async Task AnInsertWhoseRowsAreInsideTheFiltersIsSentUnchanged()
    {
        var (context, sent) = Orders(OpenOnly());
        var insert = new InsertCommand<Dictionary<string, object?>>([Row("open"), Row("open")]);

        var result = await Write(context, insert);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        sent.ShouldHaveSingleItem().ShouldBeSameAs(insert);
    }

    [Fact]
    [Trait("Category", "DataSets.FDW5069")]
    public async Task AnInsertWithARowOutsideTheFiltersIsRefusedAndNothingIsSent()
    {
        var (context, sent) = Orders(OpenOnly());

        var result = await Write(context, new InsertCommand<Dictionary<string, object?>>([Row("open"), Row("closed")]));

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("DATA-21204");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("1");
        sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Category", "DataSets.FDW5069")]
    public async Task AWriteThatCannotBeNarrowedIsRefusedOnAFilteredDataSet()
    {
        var (context, sent) = Orders(OpenOnly());
        var bulk = new Mock<IDataCommandWithInput>();
        bulk.Setup(c => c.CommandType).Returns("BulkUpsert");

        var result = await Write(context, bulk.Object);

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("DATA-21203");
        sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Category", "DataSets.FDW5069")]
    public async Task AWriteThroughADataSetWithNoFiltersIsSentUnchanged()
    {
        var (context, sent) = Orders();
        var delete = new DeleteCommand { AllRows = true };

        var result = await Write(context, delete);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        sent.ShouldHaveSingleItem().ShouldBeSameAs(delete);
    }
}
