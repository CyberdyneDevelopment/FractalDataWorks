using System;
using System.Linq;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Scripts;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>Turns a stored script into the form the API answers in.</summary>
public static class ScriptPayloadWriter
{
    /// <summary>Writes the stored script.</summary>
    /// <param name="script">The stored script.</param>
    /// <returns>The payload.</returns>
    public static ScriptPayload Write(SqlScriptConfiguration script)
    {
        ArgumentNullException.ThrowIfNull(script);
        return new ScriptPayload
        {
            Name = script.Name,
            Description = script.Description,
            Connection = script.DataStoreName,
            Kind = script.Kind,
            Text = script.ScriptText,
            Parameters = [.. script.Parameters.OrderBy(parameter => parameter.Ordinal).Select(parameter => new ScriptParameterPayload
            {
                Name = parameter.Name, Type = parameter.DataType, Required = parameter.IsRequired,
            })],
        };
    }
}
