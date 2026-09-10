using System.ComponentModel.DataAnnotations.Schema;
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
/// e.g. <c>BatchCopyPipelineConfiguration</c>) selected by <see cref="Implementation"/>.
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
public partial class EtlPipelineConfiguration : IPipelineImplementationConfiguration, IDomainConfiguration
{

    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the parent pipeline's logical Id (FK to pipe.Pipeline.Id).</summary>
    public Guid PipelineId { get; set; }

    /// <summary>Gets the domain this record is, for the engines it names.</summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Gets or sets the engine this record names (e.g. "BatchCopy", "Streaming").</summary>
    public string? Implementation { get; set; }

    /// <inheritdoc/>
    // Why it maps with no column of its own: the name is the domain's, and there is one
    // name for a configured member. The domain provider joins the domain row to this one,
    // and the join's result set is what carries it in.
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pipeline transform configurations (ETL-specific child collection of
    /// <c>pipe.PipelineOperation</c>, FK <c>EtlPipelineId</c> → pipe.EtlPipeline).
    /// </summary>
    public IList<PipelineTransformConfiguration>? Transforms { get; set; }

    /// <summary>
    /// Gets or sets the engine typed-body configuration corresponding to <see cref="Implementation"/>
    /// (e.g. <c>BatchCopyPipelineConfiguration</c>, <c>StreamingPipelineConfiguration</c>). Composed on
    /// read and cascade-saved on write by the keystone.
    /// </summary>
    public IEtlPipelineTypedConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    IImplementationConfiguration? IDomainConfiguration.ImplementationConfiguration
    {
        get => Configuration;
        set => Configuration = (IEtlPipelineTypedConfiguration?)value;
    }
}
