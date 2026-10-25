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

/// <summary>Inserts the output rows, restricted to the write columns when the load names them, plus the columns its value sources supply.</summary>
[TypeOption(typeof(LoadStepCommandBuilders), "Insert")]
public sealed class InsertLoadStepCommandBuilder : LoadStepCommandBuilderBase
{
    private const string LoadKind = "Insert";

    /// <summary>Initializes a new instance of the <see cref="InsertLoadStepCommandBuilder"/> class.</summary>
    public InsertLoadStepCommandBuilder() : base(LoadKind)
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

        var writes = LoadColumns.Writes(input.Row);
        var shaped = LoadRows.Shape(input.Owner, input.Rows, writes.Count == 0 ? null : writes, values.Value!, input.Logger);
        return shaped.IsFailure
            ? shaped.ToNewResult<IReadOnlyList<IDataCommand>>()
            : Built(new InsertCommand<IDictionary<string, object?>>(shaped.Value!));
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> RequiredCapabilities(PipelineLoadStepConfiguration row) => [];
}
