using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Operations.Endpoints;
using Fdw.Services.Calculations;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Endpoints.Tests.Lineage;

/// <summary>
/// FDW-2186: the field lineage of a data set has one row for each of its fields, whether or not a source maps it, so a field no source maps still
/// lists what reads it. A mapping of a logical field the data set does not state keeps its own row.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Etl")]
public class GetDataSetLineageFieldRowsTests
{
    private sealed class TestableEndpoint(
        DataSetConfigurationProvider dataSets, PipelineServiceConfigurationProvider provider,
        ICalculationConfigurationProvider calculations, ILogger<GetDataSetLineageEndpointBase> logger)
        : GetDataSetLineageEndpointBase(dataSets, provider, calculations, logger)
    {
        public Task<IReadOnlyList<FieldLineageResponse>> InvokeBuildFieldLineage(
            IDataSetImplementationConfiguration dataSet, IReadOnlyList<DataSetSourceConfiguration> sources, CancellationToken ct) =>
            BuildFieldLineage(dataSet, sources, ct);
    }

    private static TestableEndpoint Endpoint()
    {
        var gateways = new Mock<IConfigurationGatewayProvider>();
        return new TestableEndpoint(
            new DataSetConfigurationProvider(NullLogger<DataSetConfigurationProvider>.Instance, gateways.Object, "PlatformConfiguration", "data", "DataSet"),
            new PipelineServiceConfigurationProvider(NullLogger<PipelineServiceConfigurationProvider>.Instance, gateways.Object, "PlatformConfiguration", "pipe", "Pipeline"),
            new Mock<ICalculationConfigurationProvider>().Object,
            NullLogger<GetDataSetLineageEndpointBase>.Instance);
    }

    private static SimpleDataSetConfiguration DataSet(params DataSetFieldConfiguration[] fields) =>
        new() { Name = "AfcPlayers", Fields = [.. fields] };

    private static DataSetFieldConfiguration Field(string name, int ordinal, bool deleted = false) =>
        new() { Name = name, Ordinal = ordinal, IsDeleted = deleted };

    private static DataSetSourceConfiguration Source(params (string Logical, string Physical)[] maps) =>
        new()
        {
            SourceName = "AfcPlayersSqlSource",
            Mappings = [.. maps.Select((map, i) => new DataSetFieldMappingConfiguration { LogicalFieldName = map.Logical, PhysicalFieldName = map.Physical, Ordinal = i })],
        };

    [Fact]
    public async Task EveryFieldOfTheDataSetHasARowWhenNoSourceMapsAny()
    {
        var rows = await Endpoint().InvokeBuildFieldLineage(
            DataSet(Field("LastName", 2), Field("FirstName", 1)), [Source()], TestContext.Current.CancellationToken);

        rows.Select(row => row.LogicalField).ShouldBe(["FirstName", "LastName"]);
        rows.ShouldAllBe(row => row.Sources.Count == 0);
    }

    [Fact]
    public async Task AMappedFieldKeepsItsRowAndGainsItsSource()
    {
        var rows = await Endpoint().InvokeBuildFieldLineage(
            DataSet(Field("FirstName", 1), Field("LastName", 2)), [Source(("LastName", "last_name"))], TestContext.Current.CancellationToken);

        rows.Select(row => row.LogicalField).ShouldBe(["FirstName", "LastName"]);
        rows.Single(row => row.LogicalField == "LastName").Sources.ShouldHaveSingleItem().PhysicalField.ShouldBe("last_name");
        rows.Single(row => row.LogicalField == "FirstName").Sources.ShouldBeEmpty();
    }

    [Fact]
    public async Task ADeletedFieldHasNoRow()
    {
        var rows = await Endpoint().InvokeBuildFieldLineage(
            DataSet(Field("FirstName", 1), Field("Gone", 2, deleted: true)), [], TestContext.Current.CancellationToken);

        rows.Select(row => row.LogicalField).ShouldBe(["FirstName"]);
    }

    [Fact]
    public async Task AMappingOfAFieldTheDataSetDoesNotStateKeepsItsOwnRow()
    {
        var rows = await Endpoint().InvokeBuildFieldLineage(
            DataSet(Field("FirstName", 1)), [Source(("Extra", "extra_col"))], TestContext.Current.CancellationToken);

        rows.Select(row => row.LogicalField).ShouldBe(["FirstName", "Extra"]);
    }
}
