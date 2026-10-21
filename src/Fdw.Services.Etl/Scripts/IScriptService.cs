using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Etl.Scripts;

/// <summary>Runs a SQL script once through the data gateway (FDW-1442).</summary>
public interface IScriptService
{
    /// <summary>Runs the script with the values given.</summary>
    /// <param name="script">The script, already checked.</param>
    /// <param name="run">The values of its parameters and, for a query, the most rows to read.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>What the script answered, or the refusal or failure, which keeps the code the gateway gave it.</returns>
    Task<IGenericResult<ScriptResultPayload>> Run(ScriptPayload script, ScriptRunPayload run, CancellationToken cancellationToken = default);
}
