using Fdw.Collections;

namespace Fdw.Services.SecretManagers.Abstractions.Secrets;

/// <summary>
/// Marker interface for secret types.
/// </summary>
public interface ISecretType : ITypeOption<int, ISecretType>
{
    /// <summary>
    /// Gets whether this secret type requires secure storage.
    /// </summary>
    bool RequiresSecureStorage { get; }
}
