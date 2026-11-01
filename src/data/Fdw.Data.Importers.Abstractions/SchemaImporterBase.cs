using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Data.SchemaImporters.Abstractions.Configuration;
using Fdw.Data.SchemaImporters.Abstractions.Results;
using Fdw.Results;
using Fdw.Services.Connections;

namespace Fdw.Data.SchemaImporters.Abstractions;

/// <summary>
/// Base class for schema importer implementations.
/// </summary>
/// <typeparam name="TConfig">The strongly-typed importer configuration type.</typeparam>
public abstract class SchemaImporterBase<TConfig> : TypeOptionBase<SchemaImporterBase<TConfig>>, ISchemaImporter
    where TConfig : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaImporterBase{TConfig}"/> class.
    /// </summary>
    /// <param name="name">Name of this importer.</param>
    /// <param name="description">Description of what this importer does.</param>
    /// <param name="dataStoreType">Type of DataStore this importer targets.</param>
    protected SchemaImporterBase(string name, string description, string dataStoreType)
        : base(name, configurationKey: $"SchemaImporters:{name}", displayName: name, description: description, category: dataStoreType)
    {
        DataStoreType = dataStoreType;
    }

    /// <inheritdoc/>
    public string DataStoreType { get; }

    /// <summary>
    /// Implements the schema import logic. Derived classes must implement this.
    /// Returns the discovered <see cref="DataStoreImplementationConfiguration"/> with its paths, containers, and fields.
    /// </summary>
    public abstract Task<IGenericResult<DataStoreImplementationConfiguration>> Import(
        string source,
        SchemaImporterOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the source. Default implementation checks for null/empty.
    /// Override to add source-specific validation.
    /// </summary>
    public virtual Task<IGenericResult<bool>> Validate(
        string source,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return Task.FromResult(GenericResult<bool>.Failure(SchemaImporterResultCodes.ByName("SourceRequired")));
        }

        return Task.FromResult(GenericResult<bool>.Success(true));
    }
}
