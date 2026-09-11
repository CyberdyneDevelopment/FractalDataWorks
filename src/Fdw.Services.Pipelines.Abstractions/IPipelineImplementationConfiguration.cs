using Fdw.Configuration;

namespace Fdw.Services.Pipelines.Abstractions;

/// <summary>
/// The contract every Pipeline implementation carries.
/// </summary>
/// <remarks>
/// The marker is what keeps the domain closed: only a configuration carrying it can be registered
/// against this domain or handed back by a read of it. A <c>pipe.Pipeline</c> domain row names the
/// implementation it is, and that implementation's own row lives in its own table --
/// <c>pipe.EtlPipeline</c> for "Etl" -- linked by a <c>PipelineId</c>/<c>PipelineRowId</c> foreign
/// key. A read through the domain is already dispatched, so a caller is handed the implementation
/// rather than something to unwrap.
/// </remarks>
public interface IPipelineImplementationConfiguration : IImplementationConfiguration
{
}
