using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Execution;
using Fdw.Services.Data.Results;

namespace Fdw.Services.Data;

/// <summary>
/// The Variation kind of data set (FDW-3638): a data set that is a view of another data set, owned by a workspace (Scoped) or a user (a
/// data view).
/// </summary>
/// <remarks>
/// A variation holds no fields or sources, so the gateway has nothing of its own to run for it. It is read through the data route, which
/// resolves its chain with <see cref="DataSetResolver"/> and runs the root data set with every filter of the chain added, so a command
/// addressed to a variation by name is refused loudly and never served from the base data set without those filters. The kind is
/// stateless and holds no providers, as the other kinds do.
/// </remarks>
[TypeOption(typeof(DataSetTypes), "Variation")]
public sealed class VariationDataSetType : DataSetTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="VariationDataSetType"/> class.</summary>
    public VariationDataSetType()
        : base("Variation", "A view of another data set, filtered by its chain (read through the data route)",
            typeof(object), Array.Empty<IDataField>(), category: "DataSetStrategy")
    {
    }

    /// <inheritdoc/>
    public override IDataQuery CreateQuery() => new DataQueryBuilder<object>(Name);

    /// <inheritdoc/>
    public override Task<IGenericResult<T>> Execute<T>(
        IDataSetExecutionContext context, IDataCommand command, CancellationToken ct = default)
    {
        if (context is not DataSetExecutionContext ctx)
            return Task.FromResult(GenericResult<T>.Failure(DataServiceResultCodes.ByName("DataSetConfigurationRequired")));

        if (ctx.Config is not IDataSetVariationImplementationConfiguration variation)
            return Task.FromResult(DataSetExecutionHelpers.KindMismatch<T>(ctx, "Variation"));

        return Task.FromResult(GenericResult<T>.Failure(
            DataServiceResultCodes.ByName("DataSetVariationNotExecutable"), ctx.Logger,
            ResultDetails.Create("dataSet", variation.Name, "baseDataSetId", variation.BaseDataSetId.ToString("D"))));
    }
}
