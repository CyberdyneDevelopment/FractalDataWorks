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

/// <summary>
/// Deletes the rows the output rows match on the key columns, one delete per row, or flags them deleted instead: by marker,
/// or by the keys absent from the output rows as a single set-based command.
/// </summary>
[TypeOption(typeof(LoadStepCommandBuilders), "Delete")]
public sealed class DeleteLoadStepCommandBuilder : LoadStepCommandBuilderBase
{
    private const string LoadKind = "Delete";

    /// <summary>Initializes a new instance of the <see cref="DeleteLoadStepCommandBuilder"/> class.</summary>
    public DeleteLoadStepCommandBuilder() : base(LoadKind)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<IReadOnlyList<IDataCommand>> Build(LoadCommandInput input)
    {
        var keys = LoadColumns.Keys(input.Row);
        var absentMode = IsAbsentMode(input.Row);
        if (input.Rows.Count == 0)
            return absentMode
                ? GenericResult<IReadOnlyList<IDataCommand>>.Failure(PipelineRunLog.AbsentSetEmpty(input.Logger, input.Owner))
                : Built();

        var matched = LoadRows.Shape(input.Owner, input.Rows, keys, [], input.Logger);
        if (matched.IsFailure)
            return matched.ToNewResult<IReadOnlyList<IDataCommand>>();

        return absentMode ? Absent(input, keys, matched.Value!) : PerRow(input, keys, matched.Value!);
    }

    private static bool IsAbsentMode(PipelineLoadStepConfiguration row)
        => !string.IsNullOrWhiteSpace(row.SoftDeleteMode)
            && ReferenceEquals(Fdw.Commands.Data.SoftDeletes.SoftDeleteModes.ByName(row.SoftDeleteMode), Fdw.Commands.Data.SoftDeletes.SoftDeleteModes.AbsentFromSource);

    private static IGenericResult<IReadOnlyList<IDataCommand>> Absent(
        LoadCommandInput input, IReadOnlyList<string> keys, List<IDictionary<string, object?>> keyRows)
    {
        var absent = new Fdw.Commands.Data.SoftDeletes.AbsentKeySet(
            keys, keyRows.Select(row => (IReadOnlyDictionary<string, object?>)new ReadOnlyDictionary<string, object?>(row)).ToList());
        var soft = LoadSoftDeletes.Create(input, LoadKind, absentAllowed: true, markerAllowed: true, absent);
        return soft.IsFailure
            ? soft.ToNewResult<IReadOnlyList<IDataCommand>>()
            : Built(new DeleteCommand { SoftDelete = soft.Value });
    }

    private static IGenericResult<IReadOnlyList<IDataCommand>> PerRow(
        LoadCommandInput input, IReadOnlyList<string> keys, List<IDictionary<string, object?>> keyRows)
    {
        Fdw.Commands.Data.SoftDeletes.SoftDeleteOptions? options = null;
        if (!string.IsNullOrWhiteSpace(input.Row.SoftDeleteMode))
        {
            var soft = LoadSoftDeletes.Create(input, LoadKind, absentAllowed: true, markerAllowed: true, absent: null);
            if (soft.IsFailure)
                return soft.ToNewResult<IReadOnlyList<IDataCommand>>();
            options = soft.Value;
        }

        var commands = new List<IDataCommand>(keyRows.Count);
        foreach (var row in keyRows)
        {
            var filter = LoadKeyFilters.ForRow(input.Owner, keys, row, input.Logger);
            if (filter.IsFailure)
                return filter.ToNewResult<IReadOnlyList<IDataCommand>>();
            commands.Add(new DeleteCommand { Filter = filter.Value, SoftDelete = options });
        }

        return Built(commands);
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> RequiredCapabilities(PipelineLoadStepConfiguration row) => string.IsNullOrWhiteSpace(row.SoftDeleteMode) ? [] : [CommandCapabilityTypes.SoftDelete.Name];
}
