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
/// Matches the output rows on the key columns: inserts new keys and updates a matched row when a compare column differs,
/// keeping history when versioning is stated. A soft delete of the keys absent from the rows follows as a second command.
/// </summary>
[TypeOption(typeof(LoadStepCommandBuilders), "BulkUpsert")]
public sealed class BulkUpsertLoadStepCommandBuilder : LoadStepCommandBuilderBase
{
    private const string LoadKind = "BulkUpsert";

    /// <summary>Initializes a new instance of the <see cref="BulkUpsertLoadStepCommandBuilder"/> class.</summary>
    public BulkUpsertLoadStepCommandBuilder() : base(LoadKind)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<IReadOnlyList<IDataCommand>> Build(LoadCommandInput input)
    {
        var options = Options(input);
        if (options.IsFailure)
            return options.ToNewResult<IReadOnlyList<IDataCommand>>();

        var soft = SoftDeleteAbsent(input);
        if (soft.IsFailure)
            return soft.ToNewResult<IReadOnlyList<IDataCommand>>();
        if (input.Rows.Count == 0 && soft.Value is not null)
            return GenericResult<IReadOnlyList<IDataCommand>>.Failure(PipelineRunLog.AbsentSetEmpty(input.Logger, input.Owner));
        if (input.Rows.Count == 0)
            return Built();

        var shaped = LoadRows.Shape(input.Owner, input.Rows, options.Value!.WriteColumns, [], input.Logger);
        if (shaped.IsFailure)
            return shaped.ToNewResult<IReadOnlyList<IDataCommand>>();

        var commands = new List<IDataCommand> { new BulkUpsertCommand<IDictionary<string, object?>>(shaped.Value!, options.Value) };
        if (soft.Value is not null)
            commands.Add(soft.Value);
        return Built(commands);
    }

    private static IGenericResult<Fdw.Commands.Data.Upserts.UpsertOptions> Options(LoadCommandInput input)
    {
        var bindings = ValueBindings.Build(input.Row.ValueSources, input.Owner, input.Logger);
        if (bindings.IsFailure)
            return bindings.ToNewResult<Fdw.Commands.Data.Upserts.UpsertOptions>();

        var options = new Fdw.Commands.Data.Upserts.UpsertOptions(LoadColumns.Keys(input.Row), LoadColumns.Compares(input.Row), LoadColumns.Writes(input.Row))
        {
            Versioning = LoadVersioning.For(input.Row),
            ValueSources = bindings.Value!.Count == 0 ? null : bindings.Value,
        };
        var problem = options.FirstProblem();
        return problem is null
            ? GenericResult<Fdw.Commands.Data.Upserts.UpsertOptions>.Success(options)
            : GenericResult<Fdw.Commands.Data.Upserts.UpsertOptions>.Failure(PipelineRunLog.LoadOptionsRefused(input.Logger, input.Owner, LoadKind, problem));
    }

    // The command layer's upsert carries no soft delete, so the engine follows it with a delete that flags the keys the
    // rows no longer hold: both run in the load's unit of work.
    private static IGenericResult<DeleteCommand?> SoftDeleteAbsent(LoadCommandInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Row.SoftDeleteMode))
            return GenericResult<DeleteCommand?>.Success(null);

        var keys = LoadColumns.Keys(input.Row);
        var keyRows = LoadRows.Shape(input.Owner, input.Rows, keys, [], input.Logger);
        if (keyRows.IsFailure)
            return keyRows.ToNewResult<DeleteCommand?>();

        var absent = new Fdw.Commands.Data.SoftDeletes.AbsentKeySet(
            keys, keyRows.Value!.Select(row => (IReadOnlyDictionary<string, object?>)new ReadOnlyDictionary<string, object?>(row)).ToList());
        var soft = LoadSoftDeletes.Create(input, LoadKind, absentAllowed: true, markerAllowed: false, absent);
        return soft.IsFailure
            ? soft.ToNewResult<DeleteCommand?>()
            : GenericResult<DeleteCommand?>.Success(new DeleteCommand { SoftDelete = soft.Value });
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> RequiredCapabilities(PipelineLoadStepConfiguration row) => WithSoftDelete(row, CommandCapabilityTypes.BulkUpsert.Name);
}
