using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Configuration for a single check within a Validate transform. Cascade child of
/// <see cref="PipelineTransformConfiguration"/> (pipe.PipelineTransformCheck), ordered by <see cref="Ordinal"/>:
/// a record is asked the checks in that order and stops at the first it fails.
/// </summary>
/// <remarks>
/// Why: implements <see cref="IGenericConfiguration"/> (mirroring <see cref="PipelineTransformSortFieldConfiguration"/>)
/// so the configuration cascade-save discovers and persists this collection when the parent transform is saved.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline")]
public sealed partial class PipelineTransformCheckConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the check's name, unique within its stage. A record the check rejects is reported with it.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the condition a record must meet, in the pipeline's condition language (see
    /// <see cref="Fdw.Services.Etl.Predicates.RecordPredicates"/>). May reference <c>{{run parameters}}</c>.
    /// </summary>
    public string Expression { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the position among the checks of the same stage, counted from 1.
    /// </summary>
    public int Ordinal { get; set; }
}
