using System;
using Fdw.Configuration;
using Fdw.Services.SecretManagers.Abstractions;

namespace Fdw.Services.SecretManagers.TestDouble;

/// <summary>
/// Typed body for the <c>Synthetic</c> secret manager: reads secrets from environment variables
/// under <see cref="Prefix"/>.
/// </summary>
/// <remarks>
/// Why a test-owned backend rather than a shipped one: FDW deliberately ships no concrete
/// SecretManager <c>[ServiceTypeOption]</c> — secret custody lives with the consuming application
/// (see <c>ReferenceSecretManagers.*</c>). This suite is such a consumer, so it declares its own
/// option in its OWN assembly, exactly as the downstream-extensible model intends. That keeps the
/// non-exposure proof self-contained and keeps Aegis ignorant of any specific backend.
/// </remarks>
public sealed class SyntheticSecretManagerConfiguration : ISecretManagerConfiguration
{
    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public Guid SecretManagerId { get; set; }

    /// <summary>Gets or sets the environment-variable prefix secret keys are looked up under.</summary>
    public string Prefix { get; set; } = string.Empty;

    string IGenericConfiguration.SectionName => "SecretManagers";

    string IGenericConfiguration.ServiceType => "SecretManager";

    string? IGenericConfiguration.ServiceOptionType => SyntheticSecretManagerType.OptionName;
}
