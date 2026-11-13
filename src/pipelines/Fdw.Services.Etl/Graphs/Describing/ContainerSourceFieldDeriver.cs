using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Etl.Graphs.Describing;

/// <summary>A container source's fields are the stored fields of the container it reads (FDW-5523), when the container is stated and a reader of containers is given.</summary>
[TypeOption(typeof(ComponentFieldDerivers), "ContainerSource")]
public sealed class ContainerSourceFieldDeriver : ComponentFieldDeriverBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainerSourceFieldDeriver"/> class.</summary>
    public ContainerSourceFieldDeriver() : base("ContainerSource")
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
        table["output"] = null;

        // Why unknown: a caller that gave no reader of containers cannot say, and a container the component does not state, or states by a run parameter, is not one to read now. None of
        // these is a guess at the fields.
        if (context.ContainerFields is not { } read || !StatesFixedContainer(component))
        {
            return GenericResult<IReadOnlyDictionary<string, IReadOnlyList<GraphField>?>>.Success(table);
        }

        var fields = await read(new ContainerFieldsRequest(component.Name, component.DataStoreName!, component.PathName, component.ContainerName!), cancellationToken).ConfigureAwait(false);
        if (fields.IsFailure)
        {
            return fields.ToNewResult<IReadOnlyDictionary<string, IReadOnlyList<GraphField>?>>();
        }

        // Why a container with no stored fields is not known, and not "known to have none": a container's fields are stored when it is discovered, so a container with none has not had them
        // recorded, and saying it has none would refuse every field fed from it. No origin: a container's field is not a field of a data set, and a field with no origin is never given the
        // origin of one it merely resembles (FDW-2156).
        table["output"] = fields.Value is { Count: > 0 } found ? [.. found] : null;
        return GenericResult<IReadOnlyDictionary<string, IReadOnlyList<GraphField>?>>.Success(table);
    }

    // Why parameters are looked for: a data store, path or container named by a run parameter is chosen when the run starts (FDW-1689), so its fields cannot be read now.
    private static bool StatesFixedContainer(PipelineComponentConfiguration component) =>
        !string.IsNullOrEmpty(component.DataStoreName)
        && !string.IsNullOrEmpty(component.ContainerName)
        && !ParameterSettings.IsBound(component, "dataStore")
        && !ParameterSettings.IsBound(component, "path")
        && !ParameterSettings.IsBound(component, "container")
        && !ParameterSettings.HoldsParameter(component.DataStoreName)
        && !ParameterSettings.HoldsParameter(component.PathName)
        && !ParameterSettings.HoldsParameter(component.ContainerName);
}
