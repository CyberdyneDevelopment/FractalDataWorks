using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Security.Hashing;

/// <summary>
/// TypeCollection of available password hash algorithms.
/// </summary>
// Why: Extensible enum pattern — new algorithms (Argon2, BCrypt) can be added
// without modifying existing code. The active algorithm is selected by name
// from configuration, enabling seamless migration.
[TypeCollection(typeof(PasswordHashAlgorithmBase), typeof(IPasswordHashAlgorithm), typeof(PasswordHashAlgorithms))]
public abstract partial class PasswordHashAlgorithms
    : TypeCollectionBase<PasswordHashAlgorithmBase, IPasswordHashAlgorithm>
{
}
