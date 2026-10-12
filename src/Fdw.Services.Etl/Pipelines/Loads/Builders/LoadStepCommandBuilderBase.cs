using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Etl.Pipelines.Loads.Builders;

/// <summary>
/// Base class for the options of <see cref="LoadStepCommandBuilders"/>, using the CRTP pattern.
/// </summary>
public abstract class LoadStepCommandBuilderBase : TypeOptionBase<LoadStepCommandBuilderBase>, ILoadStepCommandBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoadStepCommandBuilderBase"/> class.
    /// </summary>
    /// <param name="name">The option name, the same as the <c>LoadStepKinds</c> option it builds.</param>
    protected LoadStepCommandBuilderBase(string name) : base(name)
    {
    }

    /// <inheritdoc/>
    public abstract IGenericResult<IReadOnlyList<IDataCommand>> Build(LoadCommandInput input);

    /// <inheritdoc/>
    public abstract IReadOnlyList<string> RequiredCapabilities(PipelineLoadStepConfiguration row);

    /// <summary>Adds the soft delete capability to a kind's own when the row states a soft delete.</summary>
    /// <param name="row">The load row.</param>
    /// <param name="own">The capability names the kind itself needs.</param>
    /// <returns>The capability names.</returns>
    protected static IReadOnlyList<string> WithSoftDelete(PipelineLoadStepConfiguration row, params string[] own)
        => string.IsNullOrWhiteSpace(row.SoftDeleteMode) ? own : [.. own, CommandCapabilityTypes.SoftDelete.Name];

    /// <summary>Wraps commands as a successful build.</summary>
    /// <param name="commands">The commands.</param>
    /// <returns>The result.</returns>
    protected static IGenericResult<IReadOnlyList<IDataCommand>> Built(params IDataCommand[] commands)
        => GenericResult<IReadOnlyList<IDataCommand>>.Success(commands);

    /// <summary>Wraps commands as a successful build.</summary>
    /// <param name="commands">The commands.</param>
    /// <returns>The result.</returns>
    protected static IGenericResult<IReadOnlyList<IDataCommand>> Built(IReadOnlyList<IDataCommand> commands)
        => GenericResult<IReadOnlyList<IDataCommand>>.Success(commands);
}
