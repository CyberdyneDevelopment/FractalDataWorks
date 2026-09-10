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
public partial class EtlPipelineConfiguration : DomainConfigurationBase, IPipelineImplementationConfiguration, IDomainConfiguration
{







}
