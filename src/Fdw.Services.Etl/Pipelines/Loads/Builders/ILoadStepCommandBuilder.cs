using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Etl.Pipelines.Loads.Builders;

/// <summary>
/// Builds the data commands of one load step kind from its configuration row and the rows it works on (FDW-1109). One
/// option per <c>LoadStepKinds</c> option, found by the kind's name, so no caller branches on a kind.
/// </summary>
public interface ILoadStepCommandBuilder : ITypeOption<int, ILoadStepCommandBuilder>
{
    /// <summary>Builds the commands, in the order they run.</summary>
    /// <param name="input">The load row, rows and resolved source.</param>
    /// <returns>The commands (none when there is nothing to do), or a failure saying why the row cannot be built.</returns>
    IGenericResult<IReadOnlyList<IDataCommand>> Build(LoadCommandInput input);

    /// <summary>
    /// Names the command capabilities a connection type must list to run this kind of load: the names of its
    /// <c>CommandCapabilityTypes</c>. The executor refuses a connection type that lacks one before any transaction opens.
    /// </summary>
    /// <param name="row">The load row, whose soft delete or versioning can need more than the kind alone.</param>
    /// <returns>The capability names.</returns>
    IReadOnlyList<string> RequiredCapabilities(PipelineLoadStepConfiguration row);
}
