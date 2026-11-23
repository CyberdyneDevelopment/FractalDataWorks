namespace Fdw.Services.Data;

/// <summary>A path of a data store, or one container of it, by name (FDW-6967).</summary>
/// <param name="StoreName">The data store's name.</param>
/// <param name="PathName">The path's name; empty for the store's own fields.</param>
/// <param name="ContainerName">The container's name; null for the path itself.</param>
public sealed record DataStoreItemAddress(string StoreName, string PathName, string? ContainerName)
{
    /// <summary>The item as store/path or store/path/container, for messages.</summary>
    /// <returns>The item's address.</returns>
    public override string ToString()
    {
        if (PathName.Length == 0) return StoreName;
        return ContainerName is null ? string.Concat(StoreName, "/", PathName) : string.Concat(StoreName, "/", PathName, "/", ContainerName);
    }
}
