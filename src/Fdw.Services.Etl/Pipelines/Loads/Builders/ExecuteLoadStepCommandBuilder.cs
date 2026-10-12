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

/// <summary>Runs a named executable container of the destination data store, with parameters from constants and the current time.</summary>
[TypeOption(typeof(LoadStepCommandBuilders), "Execute")]
public sealed class ExecuteLoadStepCommandBuilder : LoadStepCommandBuilderBase
{
    private const string LoadKind = "Execute";

    /// <summary>Initializes a new instance of the <see cref="ExecuteLoadStepCommandBuilder"/> class.</summary>
    public ExecuteLoadStepCommandBuilder() : base(LoadKind)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<IReadOnlyList<IDataCommand>> Build(LoadCommandInput input)
    {
        var bindings = ValueBindings.Build(input.Row.ValueSources, input.Owner, input.Logger);
        if (bindings.IsFailure)
            return bindings.ToNewResult<IReadOnlyList<IDataCommand>>();

        var options = new Fdw.Commands.Data.Executions.ExecuteOptions(
            bindings.Value!.Select(binding => new Fdw.Commands.Data.Executions.ExecuteParameter(binding.Column, binding.Source)).ToList());
        var problem = options.FirstProblem();
        return problem is null
            ? Built(new ExecuteCommand(options))
            : GenericResult<IReadOnlyList<IDataCommand>>.Failure(PipelineRunLog.LoadOptionsRefused(input.Logger, input.Owner, LoadKind, problem));
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> RequiredCapabilities(PipelineLoadStepConfiguration row) => [CommandCapabilityTypes.Execute.Name];
}
