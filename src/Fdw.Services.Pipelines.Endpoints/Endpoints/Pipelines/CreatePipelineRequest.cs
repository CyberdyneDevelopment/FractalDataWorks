using Fdw.Services.Pipelines.Clients.Abstractions;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Request for creating a new pipeline.
/// </summary>
public class CreatePipelineRequest : IPipelineLoadFields
{
    /// <summary>
    /// Gets or sets the pipeline name.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pipeline type (e.g., BatchCopy, Streaming).
    /// </summary>
    /// <remarks>
    /// Why: no default engine — a literal default here previously defeated
    /// <c>CreatePipelineRequestValidator</c>'s <c>PipelineType.NotEmpty()</c> rule, since an omitted
    /// value was never actually empty. A caller that omits PipelineType must be rejected, not silently
    /// routed to "BatchCopy".
    /// </remarks>
    [Required]
    public string PipelineType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source connection name.
    /// </summary>
    [Required]
    public string SourceConnectionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the destination connection name.
    /// </summary>
    [Required]
    public string DestinationConnectionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source DataSet name.
    /// </summary>
    public string? SourceDataSet { get; set; }

    /// <summary>Gets or sets the data store to read from when the source is a data store container rather than a data set (FDW-1103). Send all three of SourceDataStoreName, SourceDataStorePath and SourceContainerName, and no SourceDataSet.</summary>
    public string? SourceDataStoreName { get; set; }

    /// <summary>Gets or sets the path within that data store, as the store names it.</summary>
    public string? SourceDataStorePath { get; set; }

    /// <summary>Gets or sets the container within that path whose rows the pipeline reads.</summary>
    public string? SourceContainerName { get; set; }

    /// <summary>
    /// Gets or sets the destination DataSet name.
    /// </summary>
    public string? DestinationDataSet { get; set; }

    /// <summary>
    /// Gets or sets optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether the pipeline is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the BatchCopy load truncates the destination before inserting. Use for
    /// snapshot-replace ingests (e.g. "current earthquakes") so a repeating/scheduled run re-loads the
    /// latest snapshot instead of failing a unique index on already-loaded keys. Default false (append).
    /// </summary>
    public bool TruncateBeforeLoad { get; set; }

    /// <summary>
    /// Gets or sets the optional transform (operation) steps for this pipeline. Each entry persists as a
    /// pipe.PipelineOperation row; its <see cref="CreatePipelineTransformRequest.FieldMappings"/> persist
    /// as child pipe.PipelineTransformFieldMapping rows. Maps onto the typed body's
    /// <c>IEtlPipelineImplementationConfiguration.Transforms</c> collection consumed by the Map transform at runtime.
    /// </summary>
    public IList<CreatePipelineTransformRequest> Transforms { get; set; } = [];

    /// <summary>Gets or sets the load mode name (Append, Replace or Upsert) of the implicit load; required when there are no load steps, refused when there are.</summary>
    public string? LoadMode { get; set; }

    /// <summary>Gets or sets the options of the implicit load; required for Upsert, refused for Append and Replace and when there are load steps.</summary>
    public PipelineLoadOptions? LoadOptions { get; set; }

    /// <summary>Gets or sets the ordered load steps, which replace the single implicit load and run in one unit of work.</summary>
    public IList<PipelineLoadStep>? LoadSteps { get; set; }

    /// <summary>
    /// Gets or sets the reject steps (FDW-1142): load steps that write the records the pipeline's stages rejected, after its
    /// load, in a unit of work of their own. A BatchCopy pipeline only. Patched alone: null keeps the stored ones, an empty
    /// list removes them, a list replaces them; never part of the load unit.
    /// </summary>
    public IList<PipelineLoadStep>? RejectSteps { get; set; }

    /// <summary>Gets or sets the value sources of the implicit load; refused when there are load steps.</summary>
    public IList<PipelineLoadColumnSource>? ValueSources { get; set; }

    /// <summary>Gets or sets the declared run parameters.</summary>
    public IList<PipelineParameterDeclaration>? Parameters { get; set; }
}
