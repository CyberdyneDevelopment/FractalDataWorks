using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Types;

/// <summary>
/// Provider for reading and writing TypeCollection metadata to a database.
/// </summary>
public interface ITypesProvider
{
    /// <summary>
    /// Gets all TypeCollection metadata.
    /// </summary>
    Task<IGenericResult<IReadOnlyList<TypeCollectionMetadata>>> GetCollections(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a TypeCollection by name.
    /// </summary>
    Task<IGenericResult<TypeCollectionMetadata>> GetCollection(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all TypeOptions for a collection.
    /// </summary>
    Task<IGenericResult<IReadOnlyList<TypeOptionMetadata>>> GetOptions(int collectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists TypeCollection metadata.
    /// </summary>
    Task<IGenericResult> SaveCollection(TypeCollectionMetadata collection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists TypeOption metadata.
    /// </summary>
    Task<IGenericResult> SaveOption(TypeOptionMetadata option, CancellationToken cancellationToken = default);
}
