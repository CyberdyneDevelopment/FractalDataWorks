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

/// <summary>Copies the source data set's rows into the destination in one set-based statement, rows never passing through the engine.</summary>
[TypeOption(typeof(LoadStepCommandBuilders), "InsertFromQuery")]
public sealed class InsertFromQueryLoadStepCommandBuilder : LoadStepCommandBuilderBase
{
    private const string LoadKind = "InsertFromQuery";

    /// <summary>Initializes a new instance of the <see cref="InsertFromQueryLoadStepCommandBuilder"/> class.</summary>
    public InsertFromQueryLoadStepCommandBuilder() : base(LoadKind)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<IReadOnlyList<IDataCommand>> Build(LoadCommandInput input)
    {
        if (input.Source is null)
            return GenericResult<IReadOnlyList<IDataCommand>>.Failure(
                PipelineRunLog.LoadOptionsRefused(input.Logger, input.Owner, LoadKind, "no source data set was resolved"));

        var bindings = ValueBindings.Build(input.Row.ValueSources, input.Owner, input.Logger);
        if (bindings.IsFailure)
            return bindings.ToNewResult<IReadOnlyList<IDataCommand>>();

        var options = new Fdw.Commands.Data.Loads.InsertFromQueryOptions(LoadColumns.Writes(input.Row), input.Source)
        {
            ValueSources = bindings.Value!.Count == 0 ? null : bindings.Value,
        };
        var problem = options.FirstProblem();
        return problem is null
            ? Built(new InsertFromQueryCommand(options))
            : GenericResult<IReadOnlyList<IDataCommand>>.Failure(PipelineRunLog.LoadOptionsRefused(input.Logger, input.Owner, LoadKind, problem));
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> RequiredCapabilities(PipelineLoadStepConfiguration row) => [CommandCapabilityTypes.InsertFromQuery.Name];
}
