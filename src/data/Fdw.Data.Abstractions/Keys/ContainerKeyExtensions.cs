namespace Fdw.Data.Abstractions;

/// <summary>
/// Extension methods for <see cref="IContainerKey"/> that provide derived state checks
/// without requiring default interface implementations (which are unsupported on
/// <c>netstandard2.0</c> targets).
/// </summary>
public static class ContainerKeyExtensions
{
    /// <summary>
    /// Returns <see langword="true"/> when the key can physically enforce uniqueness on
    /// the container — that is, when it is both physical (<see cref="IContainerKey.IsPhysical"/>)
    /// and of a type that supports uniqueness (<see cref="KeyTypeBase.SupportsUniqueness"/>).
    /// </summary>
    /// <param name="key">The key to inspect.</param>
    /// <remarks>
    /// Join and Foreign keys never enforce uniqueness. Primary and Surrogate keys enforce
    /// uniqueness only when <see cref="IContainerKey.IsPhysical"/> is <see langword="true"/>.
    /// </remarks>
    public static bool CanEnforceUniqueness(this IContainerKey key)
        => key.IsPhysical && key.KeyType.SupportsUniqueness;
}
