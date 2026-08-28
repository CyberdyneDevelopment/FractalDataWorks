using Fdw.Configuration;

namespace Fdw.Services.Logging.Abstractions;

/// <summary>
/// The contract every logging implementation's configuration satisfies.
/// </summary>
/// <remarks>
/// Implementations add their own settings — sinks, output templates, per-source overrides — and the
/// domain configuration holds one of these, named by its <c>ServiceOptionType</c>.
/// </remarks>
public interface ILoggingImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the minimum level this pipeline emits, as the implementation names it.</summary>
    string MinimumLevel { get; set; }
}
