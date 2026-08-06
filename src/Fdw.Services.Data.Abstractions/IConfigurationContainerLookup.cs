using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Resolves <see cref="IDataContainer"/> nodes from the IDataStore tree by configuration type name
/// or by section path (category). Used by configuration endpoints as the authoritative source
/// of schema and table metadata — replacing the deleted <c>IConfigurationType</c> path.
/// </summary>
/// <remarks>
/// Why: After Wave C5 removes IConfigurationType, configuration endpoint base classes need another
/// way to find the right schema/table for a given configuration type name. This lookup walks the
/// Lazy&lt;IReadOnlyList&lt;IDataStore&gt;&gt; tree and matches by container Name (table) and,
/// for ByCategory, by container SectionPath discriminator metadata.
/// </remarks>
public interface IConfigurationContainerLookup
{
    /// <summary>
    /// Returns the <see cref="IDataContainer"/> whose name matches <paramref name="configTypeName"/>.
    /// </summary>
    /// <param name="configTypeName">
    /// The configuration type name (e.g., "MsSqlConnection", "JwtAuthentication").
    /// Matched case-insensitively against <see cref="IDataNode.Name"/>.
    /// </param>
    /// <returns>
    /// Success with the matching container; or failure if not found.
    /// </returns>
    IGenericResult<IDataContainer> Get(string configTypeName);

    /// <summary>
    /// Returns all <see cref="IDataContainer"/> nodes across all stores and paths.
    /// </summary>
    /// <returns>All containers in the ctrl-tier data store tree.</returns>
    IReadOnlyList<IDataContainer> All();

    /// <summary>
    /// Returns all <see cref="IDataContainer"/> nodes whose <c>SectionPath</c> metadata matches
    /// the given <paramref name="sectionPath"/>.
    /// </summary>
    /// <param name="sectionPath">
    /// The IConfiguration section path (e.g., "Connections", "DataSets") to filter by.
    /// </param>
    /// <returns>All matching containers, or an empty list if none match.</returns>
    IReadOnlyList<IDataContainer> ByCategory(string sectionPath);
}
