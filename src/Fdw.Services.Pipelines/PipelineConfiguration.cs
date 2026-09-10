using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Pipelines;

/// <summary>
/// General header configuration for pipeline services representing the pipe.Pipeline parent table.
/// The pipeline KIND lives in <see cref="Implementation"/> (e.g. "Etl") and the kind-specific
/// fields live on the <see cref="Configuration"/> typed body (e.g. <c>EtlPipelineConfiguration</c>).
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline")]
public partial class PipelineConfiguration : DomainConfigurationBase, IPipelineConfiguration
{











}
