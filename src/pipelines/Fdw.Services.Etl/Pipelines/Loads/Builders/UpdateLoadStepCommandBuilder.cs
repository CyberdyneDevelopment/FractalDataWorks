using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Etl.Logging;

namespace Fdw.Services.Etl.Pipelines.Loads.Builders;

/// <summary>
/// Updates the write columns of the row each output row matches on the key columns, the whole batch in one set-based bulk update
/// (FDW-4953), optionally flagging marker rows deleted.
/// </summary>
[TypeOption(typeof(LoadStepCommandBuilders), "Update")]
public sealed class UpdateLoadStepCommandBuilder : LoadStepCommandBuilderBase
{
    private const string LoadKind = "Update";

    /// <summary>Initializes a new instance of the <see cref="UpdateLoadStepCommandBuilder"/> class.</summary>
    public UpdateLoadStepCommandBuilder() : base(LoadKind)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<IReadOnlyList<IDataCommand>> Build(LoadCommandInput input)
    {
        if (input.Rows.Count == 0)
            return Built();

        var bindings = ValueBindings.Build(input.Row.ValueSources, input.Owner, input.Logger);
        if (bindings.IsFailure)
            return bindings.ToNewResult<IReadOnlyList<IDataCommand>>();
        var values = ValueBindings.RowValues(bindings.Value!, input.Owner, LoadKind, replacesRows: true, input.Now, input.Logger);
        if (values.IsFailure)
            return values.ToNewResult<IReadOnlyList<IDataCommand>>();

        var soft = SoftDelete(input);
        if (soft.IsFailure)
            return soft.ToNewResult<IReadOnlyList<IDataCommand>>();

        // Each row carries its keys and the columns it writes; the batch is one set-based update (FDW-4953).
        var keys = LoadColumns.Keys(input.Row);
        var writes = LoadColumns.Writes(input.Row);
        var shaped = LoadRows.Shape(input.Owner, input.Rows, Staged(keys, writes), values.Value!, input.Logger);
        if (shaped.IsFailure)
            return shaped.ToNewResult<IReadOnlyList<IDataCommand>>();
        var present = LoadKeyFilters.KeysPresent(input.Owner, keys, shaped.Value!, input.Logger);
        if (present.IsFailure)
            return present.ToNewResult<IReadOnlyList<IDataCommand>>();

        var options = new Fdw.Commands.Data.Updates.BulkUpdateOptions(keys, [.. writes, .. values.Value!.Select(value => value.Key)]);
        var problem = options.FirstProblem();
        if (problem is not null)
            return GenericResult<IReadOnlyList<IDataCommand>>.Failure(PipelineRunLog.LoadOptionsRefused(input.Logger, input.Owner, LoadKind, problem.Message));

        return Built(new BulkUpdateCommand<IDictionary<string, object?>>(shaped.Value!, options) { SoftDelete = soft.Value, UnmatchedKeySample = input.UnmatchedKeySample });
    }

    // The key columns, then the write columns that are not keys: a write column that is also a key is matched on, never set.
    private static List<string> Staged(IReadOnlyList<string> keys, IReadOnlyList<string> writes)
        => [.. keys, .. writes.Where(write => !keys.Contains(write, StringComparer.OrdinalIgnoreCase))];

    private static IGenericResult<Fdw.Commands.Data.SoftDeletes.SoftDeleteOptions?> SoftDelete(LoadCommandInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Row.SoftDeleteMode))
            return GenericResult<Fdw.Commands.Data.SoftDeletes.SoftDeleteOptions?>.Success(null);
        var soft = LoadSoftDeletes.Create(input, LoadKind, absentAllowed: false, markerAllowed: true, absent: null);
        return soft.IsFailure
            ? soft.ToNewResult<Fdw.Commands.Data.SoftDeletes.SoftDeleteOptions?>()
            : GenericResult<Fdw.Commands.Data.SoftDeletes.SoftDeleteOptions?>.Success(soft.Value);
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> RequiredCapabilities(PipelineLoadStepConfiguration row) => WithSoftDelete(row);
}
