namespace Fdw.Configuration;

/// <summary>
/// A domain configuration — one configured member of a service domain, naming which implementation it
/// is and holding that implementation's own configuration.
/// </summary>
/// <typeparam name="TImplementationConfiguration">The domain's implementation configuration contract.</typeparam>
/// <remarks>
/// Every service domain has one of these and N implementations of it. The implementation is
/// <b>held</b> — never inherited — so a domain that ships one implementation today can take a second
/// without changing the first. A domain given a single flat configuration is closed to that.
/// <para>
/// The domain configuration provider returns the list of these; the implementation configuration
/// provider registered for a member's <c>ServiceOptionType</c> supplies what
/// <see cref="Configuration"/> holds.
/// </para>
/// <para>
/// It stays this small deliberately. Health-check and discovery settings were nearly lifted here from
/// <c>ConnectionConfiguration</c>, but one of the eighteen domain tables carries them — they are the
/// connection domain's own concern and belong on its implementation contract, not on every domain.
/// </para>
/// </remarks>
public interface IPlatformServiceConfiguration<TImplementationConfiguration> : IGenericConfiguration
    where TImplementationConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets a description of this configured member.</summary>
    string? Description { get; set; }

    /// <summary>Gets or sets the implementation's own configuration.</summary>
    TImplementationConfiguration? Configuration { get; set; }
}
