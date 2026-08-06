namespace Fdw.Data.Abstractions;

/// <summary>
/// Extension methods for <see cref="IStorageContainer"/> and <see cref="IDataContainer"/>.
/// </summary>
public static class StorageContainerExtensions
{
    /// <summary>
    /// Returns the surrogate primary key field name by reading structured key metadata
    /// directly from the <see cref="IDataContainer"/>.
    /// </summary>
    /// <param name="container">The data container.</param>
    /// <returns>
    /// The first field name of the first surrogate or primary key, or <c>null</c> if none is defined.
    /// </returns>
    public static string? GetPrimaryKeyFieldName(this IDataContainer container)
    {
        if (container is null)
            return null;

        // Why: IsPrimaryKey is true for both Surrogate and PrimaryKey key types — the only
        // key types that serve as a physical or logical single-column identity. No string-keyed
        // Metadata access; structured Keys properties are the source of truth.
        var keys = container.Keys;
        for (var i = 0; i < keys.Count; i++)
        {
            var key = keys[i];
            if (key.KeyType.IsPrimaryKey && key.KeyFields.Count > 0)
                return key.KeyFields[0].LocalField.Name;
        }

        return null;
    }

    /// <summary>
    /// Returns the surrogate primary key field name for a storage container.
    /// </summary>
    /// <param name="container">The storage container.</param>
    /// <returns>
    /// The surrogate key field name from Metadata, or <c>null</c> if no surrogate key is defined.
    /// </returns>
    // Why: Retained for OData translators and other generic IStorageContainer callers that
    // do not have access to the underlying IDataContainer. MsSql translators use the
    // IDataContainer overload above to read structured keys directly.
    public static string? GetPrimaryKeyFieldName(this IStorageContainer container)
    {
        if (container?.Metadata == null)
            return null;

        if (container.Metadata.TryGetValue("SurrogateKeyField", out var pkObj) && pkObj is string pkName)
            return pkName;

        return null;
    }
}
