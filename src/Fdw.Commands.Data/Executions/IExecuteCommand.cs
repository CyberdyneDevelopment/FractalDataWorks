using Fdw.Commands.Data.Abstractions;

namespace Fdw.Commands.Data.Executions;

/// <summary>
/// Runs a pre-defined executable container of the connection with parameters. A translator reads the parameters
/// from <see cref="Options"/>.
/// </summary>
public interface IExecuteCommand : IDataCommand
{
    /// <summary>Gets the parameters and where each value comes from.</summary>
    ExecuteOptions Options { get; }
}
