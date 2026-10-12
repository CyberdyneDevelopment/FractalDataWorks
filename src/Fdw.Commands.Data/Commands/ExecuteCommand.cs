using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Executions;
using Fdw.Commands.Data.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Execute command: runs the executable container it is dispatched with, with parameters. Returns the number of
/// rows the execution affected, as the connection reports it.
/// </summary>
/// <remarks>
/// Never caller-supplied text: the container is one the data store already knows, and each parameter is a value
/// source (<see cref="ExecuteOptions"/>). What a connection type executes (a stored procedure on SQL Server) is
/// decided only by that connection type's translator; a connection type without an executable container does not
/// list the Execute capability. It carries no rows.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataCommands), "Execute", RestrictToCurrentCompilation = true)]
public sealed class ExecuteCommand : DataCommandBase<int>, IExecuteCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecuteCommand"/> class.
    /// </summary>
    /// <param name="options">The parameters and where each value comes from.</param>
    public ExecuteCommand(ExecuteOptions options)
        : base("Execute")
    {
        Options = options;
        ExecuteCommandLog.CommandCreated(NullLogger<ExecuteCommand>.Instance, options.Parameters.Count);
    }

    /// <summary>Gets the parameters and where each value comes from.</summary>
    public ExecuteOptions Options { get; }
}
