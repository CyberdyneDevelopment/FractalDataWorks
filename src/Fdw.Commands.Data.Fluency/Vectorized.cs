namespace Fdw.Commands.Data;

/// <summary>
/// Entry point for the vectorized builders: searches only a store that holds vectors answers (FDW-1354).
/// </summary>
public static class Vectorized
{
    /// <summary>
    /// Begins building a search for the rows of a container nearest a vector.
    /// </summary>
    /// <typeparam name="T">The type each row is read as.</typeparam>
    /// <param name="dataStoreName">The data store.</param>
    /// <param name="pathName">The path within the data store.</param>
    /// <param name="containerName">The container whose rows are searched.</param>
    /// <returns>A builder for the search.</returns>
    public static NearestCommandBuilder<T> Nearest<T>(string dataStoreName, string pathName, string containerName)
        => new(dataStoreName, pathName, containerName);
}
