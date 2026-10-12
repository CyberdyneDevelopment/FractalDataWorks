using System.Collections.Generic;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Represents a catalog of datasets that provides type-safe lookup and enumeration capabilities.
/// This interface enables strongly-typed dataset catalogs with various lookup methods.
/// </summary>
public interface IDataSetCatalog
{
    /// <summary>
    /// Gets all datasets in this collection.
    /// </summary>
    IReadOnlyList<IDataSetType> All { get; }

    /// <summary>
    /// Gets the total count of datasets in this collection.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets a dataset by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the dataset.</param>
    /// <returns>The dataset with the specified identifier, or null if not found.</returns>
    IDataSetType? Get(int id);

    /// <summary>
    /// Gets a dataset by its name.
    /// </summary>
    /// <param name="name">The name of the dataset.</param>
    /// <returns>The dataset with the specified name, or null if not found.</returns>
    IDataSetType? Get(string name);

    /// <summary>
    /// Gets all datasets in the specified category.
    /// </summary>
    /// <param name="category">The category to filter by.</param>
    /// <returns>A collection of datasets in the specified category.</returns>
    IReadOnlyList<IDataSetType> GetByCategory(string category);

    /// <summary>
    /// Gets all datasets that support the specified operation.
    /// </summary>
    /// <param name="supportsRead">Filter by read support capability.</param>
    /// <param name="supportsWrite">Filter by write support capability.</param>
    /// <param name="supportsDelete">Filter by delete support capability.</param>
    /// <returns>A collection of datasets that match the specified capabilities.</returns>
    IReadOnlyList<IDataSetType> GetByCapabilities(bool? supportsRead = null, bool? supportsWrite = null, bool? supportsDelete = null);
}