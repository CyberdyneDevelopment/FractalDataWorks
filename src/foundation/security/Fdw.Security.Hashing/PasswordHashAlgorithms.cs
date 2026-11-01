using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Security.Hashing;

/// <summary>
/// TypeCollection of available password hash algorithms.
/// </summary>
[TypeCollection(typeof(PasswordHashAlgorithmBase), typeof(IPasswordHashAlgorithm), typeof(PasswordHashAlgorithms))]
public abstract partial class PasswordHashAlgorithms
    : TypeCollectionBase<PasswordHashAlgorithmBase, IPasswordHashAlgorithm>
{
}
