using Fdw.Configuration;

namespace Fdw.Services.Pipelines.Abstractions;

/// <summary>
/// Marker interface for a pipeline KIND typed body (e.g. <c>EtlPipelineConfiguration</c>).
/// The general <c>PipelineConfiguration</c> header carries an
/// <c>IPipelineTypedConfiguration? Configuration</c> property whose runtime type is the kind body
/// selected by the header's <c>ServiceOptionType</c> discriminator (e.g. "Etl").
/// </summary>
/// <remarks>
/// Why: a bare <see cref="IGenericConfiguration"/>-typed <c>Configuration</c> property does NOT trigger
/// the generated mapper's <c>GetTypedBody</c>/<c>SetTypedBody</c> — the polymorphic typed-body
/// composition only fires when the property is declared as a marker interface that derives from
/// <see cref="IGenericConfiguration"/>. This marker is that trigger for the pipeline header.
/// The kind body is persisted in its own table (<c>pipe.EtlPipeline</c>) and linked to the parent
/// <c>pipe.Pipeline</c> row via a <c>PipelineId</c>/<c>PipelineRowId</c> foreign key.
/// </remarks>
public interface IPipelineTypedConfiguration : IImplementationConfiguration
{
}
