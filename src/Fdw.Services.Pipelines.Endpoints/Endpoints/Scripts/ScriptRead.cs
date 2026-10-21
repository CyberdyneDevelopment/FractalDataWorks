using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints.Logging;
using Fdw.Services.Pipelines.Scripts;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>Reads a stored script by its name.</summary>
public static class ScriptRead
{
    /// <summary>Finds the script.</summary>
    /// <param name="provider">The stored scripts.</param>
    /// <param name="name">The script's name.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The script, or null when nothing has that name.</returns>
    public static async Task<IGenericResult<ScriptPayload?>> Find(ScriptConfigurationProvider provider, string name, ILogger logger, CancellationToken ct)
    {
        var read = await provider.Get(name, ct).ConfigureAwait(false);
        if (read.IsFailure)
        {
            return read.ToNewResult<ScriptPayload?>();
        }

        if (read.Value is null)
        {
            return GenericResult<ScriptPayload?>.Success(null);
        }

        if (read.Value is not SqlScriptConfiguration script)
        {
            return GenericResult<ScriptPayload?>.Failure(ScriptEndpointLog.NotASqlScript(logger, name, read.Value.Implementation));
        }

        return GenericResult<ScriptPayload?>.Success(ScriptPayloadWriter.Write(script));
    }

    /// <summary>Reads a script that was just saved.</summary>
    /// <param name="provider">The stored scripts.</param>
    /// <param name="name">The script's name.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The script, or the failure that says it cannot be read.</returns>
    public static async Task<IGenericResult<ScriptPayload>> Read(ScriptConfigurationProvider provider, string name, ILogger logger, CancellationToken ct)
    {
        var found = await Find(provider, name, logger, ct).ConfigureAwait(false);
        if (found.IsFailure)
        {
            return found.ToNewResult<ScriptPayload>();
        }

        return found.Value is { } payload
            ? GenericResult<ScriptPayload>.Success(payload)
            : GenericResult<ScriptPayload>.Failure(ScriptEndpointLog.SavedButUnreadable(logger, name));
    }
}
