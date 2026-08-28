using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Etl;

/// <summary>
/// The ETL-KIND typed body of <c>PipelineConfiguration</c> (kind "Etl"). Persisted in
/// <c>pipe.EtlPipeline</c> as a type-specific child of <c>pipe.Pipeline</c>. Carries the ETL-specific
/// <see cref="Transforms"/> child collection and the ENGINE typed body (<see cref="Configuration"/>,
/// e.g. <c>BatchCopyPipelineConfiguration</c>) selected by <see cref="ServiceOptionType"/>.
/// </summary>
/// <remarks>
/// Why: this is the middle level of the two-level pipeline typed-body chain
/// (Pipeline → EtlPipeline → engine). It is NO LONGER the base class of the engine configurations —
/// the engines are standalone bodies implementing <see cref="IEtlPipelineTypedConfiguration"/>.
/// Properties use <c>{ get; set; }</c> to satisfy IOptions binding.
/// </remarks>
[GenerateMapper]
[ExcludeFromCodeCoverage]
[ManagedConfiguration(ServiceCategory = "Pipeline", ServiceType = "Etl")]
public partial class EtlPipelineConfiguration : IPipelineImplementationConfiguration
{

    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the parent pipeline's logical Id (FK to pipe.Pipeline.Id).</summary>
    public Guid PipelineId { get; set; }

    /// <inheritdoc/>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string SectionName { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string ServiceType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the engine discriminator (e.g. "BatchCopy", "Streaming") that drives dispatch to the
    /// engine typed body in <see cref="Configuration"/>.
    /// </summary>
    public string? ServiceOptionType { get; set; }

    /// <summary>
    /// Gets or sets the pipeline transform configurations (ETL-specific child collection of
    /// <c>pipe.PipelineOperation</c>, FK <c>EtlPipelineId</c> → pipe.EtlPipeline).
    /// </summary>
    public IList<PipelineTransformConfiguration>? Transforms { get; set; }

    /// <summary>
    /// Gets or sets the engine typed-body configuration corresponding to <see cref="ServiceOptionType"/>
    /// (e.g. <c>BatchCopyPipelineConfiguration</c>, <c>StreamingPipelineConfiguration</c>). Composed on
    /// read and cascade-saved on write by the keystone.
    /// </summary>
    public IEtlPipelineTypedConfiguration? Configuration { get; set; }
}
