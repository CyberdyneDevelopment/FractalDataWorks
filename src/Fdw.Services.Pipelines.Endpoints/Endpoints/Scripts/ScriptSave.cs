using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Scripts;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>Saves a stored script through the domain provider and reads it back as the API answers it.</summary>
public static class ScriptSave
{
    /// <summary>Saves the script.</summary>
    /// <param name="provider">The stored scripts.</param>
    /// <param name="script">The stored script, already checked.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The script as it was saved, or the failure.</returns>
    public static async Task<IGenericResult<ScriptPayload>> Save(ScriptConfigurationProvider provider, SqlScriptConfiguration script, ILogger logger, CancellationToken ct)
    {
        var saved = await provider
            .Save(script, ScriptPayloadReader.DomainName, ScriptPayloadReader.ImplementationName, script.Name, ct)
            .ConfigureAwait(false);
        if (saved.IsFailure)
        {
            return saved.ToNewResult<ScriptPayload>();
        }

        return await ScriptRead.Read(provider, script.Name, logger, ct).ConfigureAwait(false);
    }
}
