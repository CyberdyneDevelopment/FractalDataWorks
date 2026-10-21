using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Reads the run parameters a pipeline declares back into the shape the body carries (FDW-1110, contract item 5).
/// </summary>
public static class PipelineLoadReader
{
    /// <summary>Reads the declared run parameters.</summary>
    /// <param name="declaring">The pipeline's configuration, of any kind that declares run parameters.</param>
    /// <returns>The declarations, or <see langword="null"/> when the pipeline declares none.</returns>
    public static IList<PipelineParameterDeclaration>? Parameters(IDeclaresRunParameters declaring) =>
        declaring.DeclaredRunParameters is { Count: > 0 } rows ? [.. rows.Select(ReadParameter)] : null;

    private static PipelineParameterDeclaration ReadParameter(IPipelineRunParameter row) => new()
    {
        Name = row.Name,
        Type = row.ParameterType,
        Required = row.IsRequired,
        IsList = row.IsList,
        ShowsValue = row.ShowsValue,
        Description = row.Description,
    };
}
