using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Results;
using Microsoft.Extensions.Configuration;

namespace Fdw.Services.Data.Discovery;

/// <summary>
/// Interface for container discovery method TypeOptions.
/// Each TypeOption represents a strategy for discovering containers within a data store
/// (automatic discovery, file-based import, or manual definition).
/// </summary>
public interface IDiscoveryMethod : ITypeOption<int, DiscoveryMethodBase>
{
    /// <summary>
    /// Creates a new, empty instance of this discovery method type.
    /// The returned instance has { get; set; } properties ready for binding.
    /// The singleton prototype in the TypeCollection is NOT the bindable instance --
    /// call this to get one.
    /// </summary>
    DiscoveryMethodBase CreateInstance();

    /// <summary>
    /// Binds this instance's settable properties from an IConfigurationSection.
    /// Each concrete type reads only its own properties -- no switch, no reflection.
    /// Called on a fresh instance from CreateInstance(), not on the prototype.
    /// </summary>
    void Bind(IConfigurationSection section);

    /// <summary>
    /// Decomposes this instance into key-value pairs for writing to the KVP table.
    /// Includes the Type discriminator. The inverse of Bind().
    /// </summary>
    IReadOnlyList<KeyValuePair<string, string?>> AsKvp();

    /// <summary>
    /// Property names this type expects from configuration.
    /// Used by the UI to render only relevant fields and by the resolver
    /// to validate completeness.
    /// </summary>
    IReadOnlyList<string> ExpectedProperties { get; }

    /// <summary>
    /// Property names that are required (non-optional) for this type.
    /// Subset of ExpectedProperties.
    /// </summary>
    IReadOnlyList<string> RequiredProperties { get; }

    /// <summary>
    /// Whether this discovery method supports automatic schema discovery.
    /// When true, the method can discover containers without user input.
    /// </summary>
    bool SupportsAutoDiscovery { get; }

    /// <summary>
    /// Validates that all required properties are populated.
    /// Called on a bound instance (not the prototype).
    /// </summary>
    IGenericResult Validate();

    /// <summary>
    /// Whether this is the Empty/NotFound sentinel returned by failed lookups.
    /// </summary>
    bool IsEmpty { get; }
}
