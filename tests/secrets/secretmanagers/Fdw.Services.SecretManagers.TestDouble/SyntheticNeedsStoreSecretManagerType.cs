using Fdw.Collections;

namespace Fdw.Services.SecretManagers.TestDouble;

/// <summary>
/// A test secret manager kind that cannot be built from its declaration alone (FDW-6360): it reuses the Synthetic kind's body and factory and states the
/// fact the other way, so a schema that declares it is refused at start-up.
/// </summary>
[Implementation(typeof(SecretManagerTypes), OptionName)]
public sealed class SyntheticNeedsStoreSecretManagerType
    : SecretManagerTypeBase<Abstractions.ISecretManager, ISyntheticSecretManagerFactory, SyntheticSecretManagerConfiguration>
{
    /// <summary>The discriminator this option registers under.</summary>
    public const string OptionName = "SyntheticNeedsStore";

    /// <summary>Initializes a new instance of the <see cref="SyntheticNeedsStoreSecretManagerType"/> class.</summary>
    public SyntheticNeedsStoreSecretManagerType()
        : base(
            name: OptionName,
            sectionName: OptionName,
            displayName: "Synthetic needing the store (test)",
            description: "Test-owned secret manager kind that states it cannot be built from a declaration alone.",
            supportedSecretStores: [OptionName],
            supportedSecretTypes: ["ApiKey"],
            supportsRotation: false,
            supportsVersioning: false,
            supportsSoftDelete: false,
            supportsAccessPolicies: false,
            maxSecretSizeBytes: 1024,
            defaultContainerName: "SyntheticNeedsStore")
    {
    }

    /// <inheritdoc/>
    public override bool BuildableFromDeclarationAlone => false;
}
