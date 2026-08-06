using Fdw.Collections.Attributes;

namespace Fdw.Services.SecretManagers.Abstractions.Secrets;

/// <summary>
/// Represents a secret type for services that don't require any secrets.
/// </summary>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(SecretTypes), "None", RestrictToCurrentCompilation = true)]
public sealed class NoneSecretType : SecretTypeBase, ISecretType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoneSecretType"/> class.
    /// </summary>
    public NoneSecretType()
        : base(
            id: 0,
            name: "None",
            description: "No secret required",
            requiresSecureStorage: false)
    {
    }
}
