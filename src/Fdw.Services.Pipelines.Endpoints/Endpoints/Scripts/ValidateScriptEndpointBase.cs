using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>Checks a stored script draft without saving it (FDW-1442).</summary>
public abstract class ValidateScriptEndpointBase : Endpoint<ScriptPayload, ScriptValidationPayload>
{
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="ValidateScriptEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    protected ValidateScriptEndpointBase(ILogger<ValidateScriptEndpointBase> logger)
    {
        _logger = logger ?? NullLogger<ValidateScriptEndpointBase>.Instance;
    }

    /// <summary>Gets the policy a caller holds to check a draft: the one that writes scripts.</summary>
    protected virtual string WritePolicy => "scripts:write";

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/scripts/validate");
        DontThrowIfValidationFails();
#if DEVELOP
        AllowAnonymous();
#else
        Policies(WritePolicy);
#endif
        Summary(s =>
        {
            s.Summary = "Check a script draft without saving it";
            s.Description = "Takes the body of POST /scripts and answers every problem saving it would be refused for, each with the path to the part it concerns. An empty list means saving would accept the draft.";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(ScriptPayload req, CancellationToken ct)
    {
        ScriptEndpointLog.DraftCheckStarted(_logger, req.Name);
        var problems = ScriptChecker.Problems(req);
        ScriptEndpointLog.DraftCheckFinished(_logger, req.Name, problems.Count);
        await Send.OkAsync(new ScriptValidationPayload { IsValid = problems.Count == 0, Problems = [.. problems] }, ct).ConfigureAwait(false);
    }
}
