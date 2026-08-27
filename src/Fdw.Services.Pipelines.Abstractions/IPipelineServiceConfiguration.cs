using Fdw.Configuration;

namespace Fdw.Services.Pipelines.Abstractions;

/// <summary>
/// The configuration the pipeline SERVICE is resolved against — the list, not a member of it.
/// </summary>
/// <remarks>
/// Distinct from <see cref="IPipelineTypedConfiguration"/>, which is one member's own configuration.
/// This one is the root header that owns <c>Name</c> and <c>ServiceOptionType</c> and backs
/// <c>pipe.Pipeline</c>; the provider name-resolves against it and the factory takes the typed body
/// from it.
/// <para>
/// It is also what <c>EtlPipelineTypes</c> binds. That collection's options keep
/// <c>EtlPipelineConfiguration</c> as their own base config — the engine level — while the collection's
/// <c>ConfigurationType</c> names this root, because resolution starts here. Two collections, one root.
/// </para>
/// </remarks>
public interface IPipelineServiceConfiguration : IImplementationConfiguration
{
}
