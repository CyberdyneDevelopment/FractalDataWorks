using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Execution;
using Fdw.Services.Data.Transformations;
using Fdw.Commands.Data;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;
using IDataField = Fdw.Data.Abstractions.IDataField;

namespace Fdw.Services.Data.Tests.StableReads;

/// <summary>
/// Tests for streaming a Simple data set (FDW-4911): when its one source's connection opens a cursor, the data set is read through it, in
/// batches shaped as a page of the data set is shaped, so a keyless data set over a streaming connection is read rather than refused.
/// </summary>
public sealed class SimpleDataSetStreamTests
{
    private static readonly Guid ConnectionId = Guid.Parse("21111111-2222-3333-4444-555555555555");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AKeylessSimpleDataSetOverAStreamingConnectionStreamsEveryRowUnderItsOwnFieldNames()
    {
        var rows = Enumerable.Range(0, 7).Select(i => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?> { ["team_name"] = $"t{i}" }).ToList();
        var cursor = new RowListRecordSource(new RecordSchema([Field("team_name")]), rows);
        var (context, asked) = Context(Mock.Of<IDataConnection>(), cursor);

        var opened = await new SimpleDataSetType().OpenStream(context, new QueryCommand<Dictionary<string, object?>>(), batchSize: 3, TestContext.Current.CancellationToken);

        opened.IsSuccess.ShouldBeTrue(opened.CurrentMessage);
        var source = opened.Value.ShouldNotBeNull();
        source.Schema.Fields.Select(f => f.Name).ShouldBe(["Name"]);
        var names = new List<string>();
        await foreach (var record in source.Read(TestContext.Current.CancellationToken))
        {
            record.IsSuccess.ShouldBeTrue(record.CurrentMessage);
            names.Add((string)record.Value["Name"]!);
        }

        names.ShouldBe(Enumerable.Range(0, 7).Select(i => $"t{i}"));
        asked().ShouldBeAssignableTo<IQueryCommand>().ShouldNotBeNull().Paging.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ASimpleDataSetOverAConnectionThatCannotStreamAnswersThatItCannotBeStreamed()
    {
        var (context, _) = Context(Mock.Of<IDataConnection>(), cursor: null);

        var opened = await new SimpleDataSetType().OpenStream(context, new QueryCommand<Dictionary<string, object?>>(), batchSize: 3, TestContext.Current.CancellationToken);

        opened.IsSuccess.ShouldBeTrue(opened.CurrentMessage);
        opened.Value.ShouldBeNull();
    }

    // A Simple data set "Teams" whose one field, Name, is the source's team_name; the source's connection opens the cursor given, or none.
    private static (DataSetExecutionContext Context, Func<IDataCommand?> Asked) Context(IDataConnection plain, IRecordSource<DataRecord>? cursor)
    {
        var container = new Mock<IDataContainer> { DefaultValue = DefaultValue.Mock };
        container.Setup(c => c.Parent.Store.ConnectionId).Returns(ConnectionId);
        var stores = new Mock<IDataStoreProvider>();
        stores.Setup(s => s.Get("Store", "dbo", "Team", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataContainer>.Success(container.Object));

        IDataCommand? asked = null;
        IDataConnection connection = plain;
        if (cursor is not null)
        {
            var streaming = new Mock<IDataConnection>();
            streaming.As<IRecordSourceConnection>()
                .Setup(c => c.OpenRecordSource(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
                .Callback<IDataCommand, IDataContainer, CancellationToken>((command, _, _) => asked = command)
                .ReturnsAsync(GenericResult<IRecordSource<DataRecord>>.Success(cursor));
            connection = streaming.Object;
        }

        var connections = new Mock<IDataConnectionProvider>();
        connections.Setup(c => c.Get(ConnectionId, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IDataConnection>.Success(connection));

        // The mapping has no transformation attached: the resolver reads the attachments and finds none.
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

        var config = new SimpleDataSetConfiguration
        {
            Name = "Teams",
            Implementation = "Simple",
            Fields = [new DataSetFieldConfiguration { Name = "Name", Ordinal = 0 }],
            Sources =
            [
                new DataSetSourceConfiguration
                {
                    Name = "TeamsSource",
                    SourceName = "TeamsSource",
                    DataStoreName = "Store",
                    PathValue = "dbo",
                    ContainerName = "Team",
                    Mappings =
                    [
                        new DataSetFieldMappingConfiguration
                        {
                            Id = Guid.CreateVersion7(), Name = "Name", LogicalFieldName = "Name", PhysicalFieldName = "team_name",
                        },
                    ],
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
            () => asked);
    }

    private static IDataField Field(string name) => Mock.Of<IDataField>(field => field.Name == name);
}
