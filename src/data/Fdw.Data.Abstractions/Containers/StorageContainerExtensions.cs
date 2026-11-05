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
    public static string? GetPrimaryKeyFieldName(this IStorageContainer container)
    {
        if (container?.Metadata == null)
            return null;

        if (container.Metadata.TryGetValue("SurrogateKeyField", out var pkObj) && pkObj is string pkName)
            return pkName;

        return null;
    }

    /// <summary>
    /// Returns the field a page of this container's rows follows when the query states no ordering: the field of the primary key the
    /// container declares, whether as a key of a data container or as the surrogate key in its metadata (FDW-3454).
    /// </summary>
    /// <param name="container">The storage container.</param>
    /// <returns>The declared key's field name, or <c>null</c> when the container declares none; a caller refuses the page then, because no order is assumed.</returns>
    public static string? GetDeclaredOrderFieldName(this IStorageContainer container)
    {
        if (container is IDataContainer dataContainer && dataContainer.GetPrimaryKeyFieldName() is { } declaredKey)
            return declaredKey;

        return container.GetPrimaryKeyFieldName();
    }
}
