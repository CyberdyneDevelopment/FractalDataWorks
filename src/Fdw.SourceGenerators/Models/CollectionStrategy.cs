namespace Fdw.SourceGenerators.Models;

/// <summary>
/// Defines the collection generation strategy based on attribute type.
/// </summary>
/// <remarks>
/// Pragma suppression required: source generators cannot use TypeCollections (bootstrapping problem).
/// </remarks>
#pragma warning disable FDW017
public enum CollectionStrategy
{
    /// <summary>
    /// Immutable collection using FrozenDictionary (TypeCollectionAttribute).
    /// </summary>
    Immutable = 0,

    /// <summary>
    /// Mutable collection using ConcurrentDictionary with Register() method (MutableTypeCollectionAttribute).
    /// </summary>
    Mutable = 1,

    /// <summary>
    /// Factory collection using Dictionary with Register() method, creates new instances (TypeInstanceCollectionAttribute).
    /// </summary>
    Factory = 2
}
#pragma warning restore FDW017
