using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.SecretManagers.Abstractions.Secrets;

/// <summary>
/// TypeCollection for all secret type implementations.
/// Secret types identify what kind of credential a service requires.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(SecretTypeBase), typeof(ISecretType), typeof(SecretTypes), RestrictToCurrentCompilation = false)]
public sealed partial class SecretTypes : TypeCollectionBase<SecretTypeBase, ISecretType>
{
    // TypeCollectionGenerator will generate all members including:
    // - public static NoneSecretType None => ...
    // - All()
    // - ById()
    // - ByName()
}
