namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// Summary DTO for a registered ETL pipeline engine type (e.g., "BatchCopy", "Streaming"),
/// sourced from the <c>EtlPipelineTypes</c> ServiceTypeCollection.
/// </summary>
/// <remarks>
/// Why only <see cref="Name"/> and <see cref="Category"/> are included: these are the only members
/// exposed by the shared, non-generic <c>IEtlPipelineType</c> interface (via <c>IServiceType</c> /
/// <c>ITypeOption</c>) that <c>EtlPipelineTypes.All()</c> returns values as. The concrete
/// <c>TypeOptionBase</c> DOES implement <c>DisplayName</c>/<c>Description</c> with real, per-type
/// values (e.g. "Batch Copy" / "Batch copy pipeline for ETL operations with configurable
/// parallelism"), but neither member is declared on <c>ITypeOption</c>/<c>IServiceType</c>. A
/// pluggable enumeration endpoint — one that must keep working for ANY future
/// <c>[ServiceTypeOption(typeof(EtlPipelineTypes), "...")]</c> registered by a downstream assembly,
/// with no code change here — can only read what the shared interface declares; reaching
/// DisplayName/Description would require an unsafe per-concrete-type cast that breaks the moment an
/// engine type closes a different generic factory (which BatchCopyPipelineType/StreamingPipelineType
/// already do). Widening <c>ITypeOption</c>/<c>IServiceType</c> to declare DisplayName/Description is
/// a framework-wide interface change — out of scope here; reported as a gap rather than papered over
/// with a <c>DisplayName = Name</c> fallback.
/// </remarks>
public sealed class PipelineTypeSummary
{
    /// <summary>Gets or sets the pipeline engine type name (e.g., "BatchCopy", "Streaming").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the category for this engine type (e.g., "ETL").</summary>
    public string Category { get; set; } = string.Empty;
}
