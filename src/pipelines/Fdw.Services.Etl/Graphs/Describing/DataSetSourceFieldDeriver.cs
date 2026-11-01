using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Etl.Graphs.Describing;

/// <summary>The fields of the data set a source reads.</summary>
[TypeOption(typeof(ComponentFieldDerivers), "DataSetSource")]
public sealed class DataSetSourceFieldDeriver : ComponentFieldDeriverBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetSourceFieldDeriver"/> class.</summary>
    public DataSetSourceFieldDeriver() : base("DataSetSource")
    {
    }

    /// <inheritdoc />
    public override async Task<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<GraphField>?>>> Derive(
        PipelineComponentConfiguration component,
        IReadOnlyDictionary<string, IReadOnlyList<GraphField>?> inputs,
        DescribeContext context,
        CancellationToken cancellationToken = default)
    {
        var table = NewTable();

        // Why unknown: a data set named by a run parameter is chosen when the run starts, so its fields cannot be read now (FDW-1689).
        if (ParameterSettings.IsBound(component, "dataSet") || component.DataSetName is not { } dataSet || ParameterSettings.HoldsParameter(dataSet))
        {
            table["output"] = null;
            return GenericResult<IReadOnlyDictionary<string, IReadOnlyList<GraphField>?>>.Success(table);
        }

        var read = await context.DataSetFields(dataSet, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
        {
            return read.ToNewResult<IReadOnlyDictionary<string, IReadOnlyList<GraphField>?>>();
        }

        // Why the origin is set here: this is where a field enters the flow from a data set, so every later node can say which field it came from (FDW-2156).
        table["output"] = read.Value is { } fields ? [.. fields.Select(field => field with { Origin = new GraphFieldOrigin(dataSet, field.Name) })] : null;
        return GenericResult<IReadOnlyDictionary<string, IReadOnlyList<GraphField>?>>.Success(table);
    }
}
