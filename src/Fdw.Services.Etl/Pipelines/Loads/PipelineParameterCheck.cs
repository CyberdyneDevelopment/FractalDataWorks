using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// Judges parameter values a caller supplies for a pipeline -- a schedule's -- against the parameters the pipeline
/// declares as the API states them (FDW-1110, contract v2 item 9), by the same rules a trigger is judged by:
/// <see cref="PipelineRunParameterValues.Resolve"/>. An undeclared name, a missing required parameter and a value its
/// declared type cannot parse are refused with the parameter named.
/// </summary>
public static class PipelineParameterCheck
{
    /// <summary>Checks the supplied values against the declarations.</summary>
    /// <param name="declared">The pipeline's declared parameters, or <see langword="null"/> when it declares none.</param>
    /// <param name="supplied">The values supplied, or <see langword="null"/> when none are.</param>
    /// <param name="logger">The logger the refusals are written to.</param>
    /// <returns>Success, or a failure whose message names the parameter and the reason.</returns>
    public static IGenericResult Check(
        IList<PipelineParameterDeclaration>? declared,
        IList<PipelineParameterValue>? supplied,
        ILogger logger)
    {
        var rows = declared is null ? null : (IList<PipelineRunParameterConfiguration>)[.. declared.Select(ToRow)];
        var values = supplied is null
            ? []
            : (IReadOnlyList<KeyValuePair<string, string>>)[.. supplied.Select(v => new KeyValuePair<string, string>(v.Name, v.Value))];
        var resolved = PipelineRunParameterValues.Resolve(rows, values, logger);
        return resolved.IsFailure ? resolved : GenericResult.Success();
    }

    private static PipelineRunParameterConfiguration ToRow(PipelineParameterDeclaration declaration) => new()
    {
        Name = declaration.Name,
        ParameterType = declaration.Type,
        IsRequired = declaration.Required,
        Description = declaration.Description,
    };
}
