using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Scripts;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Scripts;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Runs a stored SQL script once with the values given (FDW-1442): a query answers up to the rows asked for, a command answers how
/// many rows it affected. Only a DataEngineer holds the policy, and the data gateway checks the role again.
/// </summary>
public abstract class RunScriptEndpointBase : ScriptRunEndpointBase<ScriptRunRequest>
{
    private readonly ScriptConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="RunScriptEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The stored scripts.</param>
    /// <param name="scripts">Runs a script through the data gateway.</param>
    protected RunScriptEndpointBase(ILogger<RunScriptEndpointBase> logger, ScriptConfigurationProvider provider, IScriptService scripts)
        : base(logger, scripts)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/scripts/{Name}/run");
        ConfigureRun(
            "Run a stored script",
            "Runs the stored script once on its data store with the values given for its parameters. A query states the most rows to read (maxRows); a command states none.");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(ScriptRunRequest req, CancellationToken ct)
    {
        var found = await ScriptRead.Find(_provider, req.Name, RunLogger, ct).ConfigureAwait(false);
        if (found.IsFailure)
        {
            await Answer(found.ToNewResult<ScriptResultPayload>(), ct).ConfigureAwait(false);
            return;
        }

        if (found.Value is not { } script)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        await Answer(await Scripts.Run(script, req, ct).ConfigureAwait(false), ct).ConfigureAwait(false);
    }
}
