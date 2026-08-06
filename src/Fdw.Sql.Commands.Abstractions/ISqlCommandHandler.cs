using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Sql.Commands.Abstractions;

/// <summary>Orchestrates command execution: looks up the translator, runs it, and propagates the result.</summary>
public interface ISqlCommandHandler
{
    /// <summary>Execute a command. Returns the translator's result.</summary>
    Task<IGenericResult<ISqlCommandResult>> Execute(ISqlCommand command, CancellationToken cancellationToken = default);
}
