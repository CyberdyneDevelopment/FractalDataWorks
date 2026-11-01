using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Scripts;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Runs a script that is not saved, once, with the values given (FDW-1442), so a data engineer can try a draft before storing it.
/// The draft is checked as saving it would check it.
/// </summary>
public abstract class RunDraftScriptEndpointBase : ScriptRunEndpointBase<ScriptDraftRunPayload>
{
    /// <summary>Initializes a new instance of the <see cref="RunDraftScriptEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="scripts">Runs a script through the data gateway.</param>
    protected RunDraftScriptEndpointBase(ILogger<RunDraftScriptEndpointBase> logger, IScriptService scripts) : base(logger, scripts)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/scripts/run");
        ConfigureRun(
            "Run a script draft",
            "Runs the script in the body once, without saving it, with the values given for its parameters. A query states the most rows to read (maxRows); a command states none.");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(ScriptDraftRunPayload req, CancellationToken ct)
    {
        var problems = ScriptChecker.Problems(req.Script);
        if (problems.Count > 0)
        {
            await Answer(ScriptRefusal.Of<ScriptResultPayload>(string.Join("; ", problems.Select(problem => problem.Reason))), ct).ConfigureAwait(false);
            return;
        }

        await Answer(await Scripts.Run(req.Script, req, ct).ConfigureAwait(false), ct).ConfigureAwait(false);
    }
}
