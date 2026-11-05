using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Graphs.Describing;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// Refuses a graph whose destination fills a column with a value source that mints a Guid (FDW-3400) when the data set field of that column
/// is not of type Guid, naming the column.
/// </summary>
/// <remarks>
/// Why a check apart from the load validator: the load validator judges what a load row says about itself and reads no data. The type of a
/// column is held by the destination's data set, so the endpoints that save and validate a graph read the fields and pass the reader in, as the
/// describe of a graph does. The rule is the kind's <c>RequiresGuidColumn</c>, so no kind is named here. A data set that cannot be read, or that
/// the destination names by variable, is not judged here (the run judges it); the first is logged.
/// </remarks>
public static class DestinationGuidColumns
{
    /// <summary>Lists the destinations whose Guid-minting value sources fill a column that is not a Guid field.</summary>
    /// <param name="graph">The graph.</param>
    /// <param name="dataSetFields">Reads the fields of a data set by name.</param>
    /// <param name="logger">The logger the refusals are written to.</param>
    /// <param name="cancellationToken">Cancels the reads.</param>
    /// <returns>A problem for each such column, placed on the destination's <c>valueSources</c> setting.</returns>
    public static async Task<IReadOnlyList<GraphProblem>> Problems(
        GraphPipelineConfiguration graph,
        Func<string, CancellationToken, Task<IGenericResult<IReadOnlyList<GraphField>>>> dataSetFields,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(dataSetFields);
        ArgumentNullException.ThrowIfNull(logger);
        var problems = new List<GraphProblem>();
        foreach (var component in graph.Components.Where(component => component.LoadKind is not null && !string.IsNullOrWhiteSpace(component.DataSetName)))
        {
            var guidSources = component.ValueSources.Where(source => ValueSourceKinds.ByName(source.SourceKind).RequiresGuidColumn).ToList();
            if (guidSources.Count == 0 || VariableText.Names(component.DataSetName!).Count > 0)
            {
                continue;
            }

            var owner = string.Concat("graph destination '", component.Name, "'");
            var read = await dataSetFields(component.DataSetName!, cancellationToken).ConfigureAwait(false);
            if (read.IsFailure || read.Value is null)
            {
                PipelineLoadLog.ValueSourceColumnsNotChecked(logger, owner, component.DataSetName!);
                continue;
            }

            foreach (var source in guidSources)
            {
                var reason = NotGuidReason(read.Value, source.ColumnName, component.DataSetName!);
                if (reason is not null)
                {
                    problems.Add(new GraphProblem(
                        ["components", component.Name, "settings", "valueSources"],
                        GenericResult.Failure(PipelineLoadLog.ValueSourceColumnNotGuid(logger, owner, source.SourceKind, source.ColumnName, reason)).CurrentMessage!));
                }
            }
        }

        return problems;
    }

    private static string? NotGuidReason(IReadOnlyList<GraphField> fields, string column, string dataSet)
    {
        var field = fields.FirstOrDefault(candidate => string.Equals(candidate.Name, column, StringComparison.OrdinalIgnoreCase));
        if (field is null)
        {
            return $"it is not a field of data set '{dataSet}'";
        }

        if (field.Type is null)
        {
            return $"its field in data set '{dataSet}' has no type";
        }

        return ReferenceEquals(DataTypes.ByName(field.Type), DataTypes.Guid)
            ? null
            : $"its field in data set '{dataSet}' is of type {field.Type}, not Guid";
    }
}
