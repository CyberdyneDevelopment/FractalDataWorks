using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataContainers.Abstractions;
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
/// What a read through a Simple data set hands to its source.
/// </summary>
/// <remarks>
/// Why these exist (FDW-877): the strategy took the caller's filter, ordering and paging with
/// <c>command is QueryCommand&lt;T&gt;</c>, where T is the RESULT type. A caller asking for
/// <c>IEnumerable&lt;IDataRow&gt;</c> with a <c>QueryCommand&lt;IDataRow&gt;</c> never matched, so every
/// data set read ran unfiltered and unpaged, and nothing tested it. The source here is a mocked
/// connection that records the command it receives.
/// </remarks>
public sealed class SimpleDataSetTypeTests
{
    private static readonly Guid ConnectionId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    private static (DataSetExecutionContext Context, Func<IDataCommand?> Received) BuildContext()
    {
        var container = new Mock<IDataContainer> { DefaultValue = DefaultValue.Mock };
        container.Setup(c => c.Parent.Store.ConnectionId).Returns(ConnectionId);

        var stores = new Mock<IDataStoreProvider>();
        stores.Setup(s => s.Get("Store", "dbo", "Team", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataContainer>.Success(container.Object));

        IDataCommand? received = null;
        var connection = new Mock<IDataConnection>();
        connection.Setup(c => c.Execute<IEnumerable<IDataRow>>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, IDataContainer, CancellationToken>((command, _, _) => received = command)
            .ReturnsAsync(GenericResult<IEnumerable<IDataRow>>.Success(Array.Empty<IDataRow>()));

        var connections = new Mock<IDataConnectionProvider>();
        connections.Setup(c => c.Get(ConnectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Success(connection.Object));

        // The source has no field mappings, so the resolver returns before it reads anything; its
        // collaborators are never called.
        var gateways = new Mock<IConfigurationGatewayProvider>();
        var resolver = new FieldMappingTransformationResolver(
            gateways.Object,
            new TransformationConfigurationProvider(
                NullLogger<TransformationConfigurationProvider>.Instance, gateways.Object, "PlatformConfiguration", "transform", "Transformation"));

        var config = new SimpleDataSetConfiguration
        {
            Name = "Teams",
            Implementation = "Simple",
            Sources =
            [
                new DataSetSourceConfiguration
                {
                    Name = "TeamsSource",
                    SourceName = "TeamsSource",
                    DataStoreName = "Store",
                    PathValue = "dbo",
                    ContainerName = "Team",
                },
            ],
        };

        return (
            new DataSetExecutionContext(
                config,
                connections.Object,
                stores.Object,
                new PredicatePushdownAnalyzer(NullLogger<PredicatePushdownAnalyzer>.Instance),
                resolver,
                NullLogger.Instance),
            () => received);
    }

    [Fact]
    [Trait("Category", "DataSets.FDW877")]
    public async Task ReadAsRowsCarriesTheCallersFilterAndPagingToTheSource()
    {
        var (context, received) = BuildContext();
        var query = new QueryCommand<IDataRow>
        {
            Filter = new FilterExpression
            {
                Root = new FilterCondition
                {
                    PropertyName = "Conference",
                    Operator = FilterOperators.ByName("Equal"),
                    Value = "AFC",
                },
            },
            Paging = new PagingExpression { Skip = 0, Take = 4 },
        };

        var result = await new SimpleDataSetType()
            .Execute<IEnumerable<IDataRow>>(context, query, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var sent = received().ShouldBeAssignableTo<IQueryCommand>();
        sent.ShouldNotBeNull();
        sent.Filter.ShouldNotBeNull("the caller's filter must reach the source");
        sent.Paging.ShouldNotBeNull("the caller's paging must reach the source");
        sent.Paging.Take.ShouldBe(4);
    }

    [Fact]
    [Trait("Category", "DataSets.FDW877")]
    public async Task ReadWithNoFilterSendsNoFilter()
    {
        var (context, received) = BuildContext();

        var result = await new SimpleDataSetType()
            .Execute<IEnumerable<IDataRow>>(context, new QueryCommand<IDataRow>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var sent = received().ShouldBeAssignableTo<IQueryCommand>();
        sent.ShouldNotBeNull();
        sent.Filter.ShouldBeNull();
        sent.Paging.ShouldBeNull();
    }
}
