using System;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Etl.Scripts;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Scripts;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>Turns a script in a request into the stored form, after checking it.</summary>
public static class ScriptPayloadReader
{
    /// <summary>The domain every stored script belongs to.</summary>
    public const string DomainName = "Script";

    /// <summary>The one implementation a stored script has.</summary>
    public const string ImplementationName = "Sql";

    /// <summary>Reads the payload as the stored script of the identity given.</summary>
    /// <param name="payload">The script in the request.</param>
    /// <param name="id">The identity the stored script takes: a new one, or the one a save replaces.</param>
    /// <returns>The stored script, or the refusal that names the first problem.</returns>
    public static IGenericResult<SqlScriptConfiguration> Read(ScriptPayload payload, Guid id)
    {
        ArgumentNullException.ThrowIfNull(payload);
        var problems = ScriptChecker.Problems(payload);
        if (problems.Count > 0)
        {
            return ScriptRefusal.Of<SqlScriptConfiguration>(string.Join("; ", problems.Select(problem => problem.Reason)));
        }

        var script = new SqlScriptConfiguration
        {
            Id = id,
            Name = payload.Name,
            Domain = DomainName,
            Implementation = ImplementationName,
            DataStoreName = payload.Connection,
            Kind = payload.Kind,
            ScriptText = payload.Text,
            Description = payload.Description,
        };
        var ordinal = 0;
        foreach (var parameter in payload.Parameters)
        {
            script.Parameters.Add(new SqlScriptParameterConfiguration
            {
                Ordinal = ordinal++, Name = parameter.Name, DataType = parameter.Type, IsRequired = parameter.Required,
            });
        }

        return GenericResult<SqlScriptConfiguration>.Success(script);
    }
}
