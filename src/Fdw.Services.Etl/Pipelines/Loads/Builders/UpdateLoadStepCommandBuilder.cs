using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Etl.Logging;

namespace Fdw.Services.Etl.Pipelines.Loads.Builders;

/// <summary>Updates the write columns of the row each output row matches on the key columns, one update per row, optionally flagging marker rows deleted.</summary>
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
        var values = ValueBindings.RowValues(bindings.Value!, input.Owner, LoadKind, input.Now, input.Logger);
        if (values.IsFailure)
            return values.ToNewResult<IReadOnlyList<IDataCommand>>();

        var soft = SoftDelete(input);
        if (soft.IsFailure)
            return soft.ToNewResult<IReadOnlyList<IDataCommand>>();

        var keys = LoadColumns.Keys(input.Row);
        var data = LoadRows.Shape(input.Owner, input.Rows, LoadColumns.Writes(input.Row), values.Value!, input.Logger);
        var matched = LoadRows.Shape(input.Owner, input.Rows, keys, [], input.Logger);
        if (data.IsFailure || matched.IsFailure)
            return data.IsFailure ? data.ToNewResult<IReadOnlyList<IDataCommand>>() : matched.ToNewResult<IReadOnlyList<IDataCommand>>();

        var commands = new List<IDataCommand>(data.Value!.Count);
        for (var index = 0; index < data.Value.Count; index++)
        {
            var filter = LoadKeyFilters.ForRow(input.Owner, keys, matched.Value![index], input.Logger);
            if (filter.IsFailure)
                return filter.ToNewResult<IReadOnlyList<IDataCommand>>();
            commands.Add(new UpdateCommand<IDictionary<string, object?>>(data.Value[index]) { Filter = filter.Value, SoftDelete = soft.Value });
        }

        return Built(commands);
    }

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
    public override IReadOnlyList<string> RequiredCapabilities(PipelineLoadStepConfiguration row) => WithSoftDelete(row, CommandCapabilityTypes.Update.Name);
}
