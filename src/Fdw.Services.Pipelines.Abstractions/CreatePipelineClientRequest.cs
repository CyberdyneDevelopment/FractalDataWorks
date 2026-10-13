using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// Client-side request to create a new pipeline.
/// </summary>
public class CreatePipelineClientRequest : IPipelineLoadFields
{
    /// <summary>
    /// Gets or sets the pipeline name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pipeline type (e.g., BatchCopy, Streaming).
    /// </summary>
    /// <remarks>
    /// Why: no default engine — a caller (or a JSON body) that omits this value must be rejected by
    /// the server's <c>PipelineType.NotEmpty()</c> validator, not silently routed to "BatchCopy". A
    /// literal default here previously defeated that validator, since the property was never actually
    /// empty. <c>PipelineCreateRequestProjection.ToCreateRequest</c> fails loud before this DTO is ever
    /// constructed from a canvas with no resolved engine.
    /// </remarks>
    public string PipelineType { get; set; } = string.Empty;

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

    /// <summary>Gets or sets the data store to write to when the destination is a data store container rather than a data set (FDW-1269). Send all three of DestinationDataStoreName, DestinationDataStorePath and DestinationContainerName, and no DestinationDataSet.</summary>
    public string? DestinationDataStoreName { get; set; }

    /// <summary>Gets or sets the path within that data store, as the store names it.</summary>
    public string? DestinationDataStorePath { get; set; }

    /// <summary>Gets or sets the container within that path that the pipeline writes its rows into.</summary>
    public string? DestinationContainerName { get; set; }

    /// <summary>
    /// Gets or sets optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether the pipeline is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether each run empties the destination before loading. Mirrors the server's
    /// <c>CreatePipelineRequest.TruncateBeforeLoad</c>; without it here the API proxy dropped the flag.
    /// </summary>
    public bool TruncateBeforeLoad { get; set; }

    /// <summary>
    /// Gets or sets the optional transform (operation) steps for this pipeline. Field names mirror
    /// the server's <c>CreatePipelineRequest.Transforms</c> exactly so the JSON round-trips.
    /// </summary>
    public IList<PipelineTransformClientRequest> Transforms { get; set; } = [];

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
