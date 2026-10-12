using System.Collections.Generic;
using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// The single transform step a pipeline field-mapping create request can express.
/// </summary>
/// <remarks>
/// Why parameters are always empty here: the create request has no field to carry them. Reporting an
/// empty set is the truth about the request, and a transform that requires a parameter is rejected at
/// execution with a message naming what is missing — rather than running with values nobody supplied.
/// </remarks>
internal sealed class PipelineFieldMappingTransform : IFieldMappingTransform
{
    /// <summary>Initializes a new instance of the <see cref="PipelineFieldMappingTransform"/> class.</summary>
    /// <param name="transformType">The registered transform name to run.</param>
    public PipelineFieldMappingTransform(string transformType) => TransformType = transformType;

    /// <inheritdoc/>
    public string TransformType { get; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> Parameters { get; } =
        new Dictionary<string, string>(System.StringComparer.Ordinal);

    /// <inheritdoc/>
    public int Ordinal => 0;
}
