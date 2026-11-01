using System;
using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Configuration;

/// <summary>
/// Reads the keys a container declares, from the data store's configuration.
/// </summary>
internal static class ConfigurationKeys
{
    /// <summary>Finds the local field of the first key of a type that identifies a row of its own container.</summary>
    /// <param name="keys">The keys the container declares.</param>
    /// <param name="keyTypeName">The key type: <c>Physical</c> or <c>Logical</c>.</param>
    /// <returns>The key's first local field, or null when the container declares no such key.</returns>
    /// <remarks>A Logical key that references another container is a Foreign key to that container's durable Id, not this container's identity.</remarks>
    internal static string? FieldOf(IReadOnlyList<IContainerKey> keys, string keyTypeName)
    {
        for (var i = 0; i < keys.Count; i++)
        {
            var key = keys[i];
            if (!string.Equals(key.KeyType.Name, keyTypeName, StringComparison.Ordinal)) continue;
            if (string.Equals(keyTypeName, "Logical", StringComparison.Ordinal) && key.ReferencedContainer is not null) continue;
            if (key.KeyFields.Count == 0) continue;
            return key.KeyFields[0].LocalField.Name;
        }

        return null;
    }
}
