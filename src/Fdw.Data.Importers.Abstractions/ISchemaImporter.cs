using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.SchemaImporters.Abstractions.Configuration;
using Fdw.Results;
using Fdw.Services.Connections;

namespace Fdw.Data.SchemaImporters.Abstractions;

/// <summary>
/// Base interface for schema importers that discover schema from external sources.
/// Implementations discovered via [TypeOption(typeof(SchemaImporters))] attribute.
/// </summary>
public interface ISchemaImporter
{
    /// <summary>
    /// Gets the unique identifier for this importer.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Gets the name of this importer.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of this importer.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the data store type this importer works with.
    /// </summary>
    string DataStoreType { get; }

    /// <summary>
    /// Imports schema from the specified source and returns a discovered DataStore configuration.
    /// </summary>
    /// <param name="source">The source to import from (connection string, URL, file path).</param>
    /// <param name="options">Optional configuration options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="DataStoreConfiguration"/> with discovered paths, containers, and fields, or a failure result.
    /// </returns>
    Task<IGenericResult<DataStoreConfiguration>> Import(
        string source,
        SchemaImporterOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the source is accessible and can be imported.
    /// </summary>
    Task<IGenericResult<bool>> Validate(
        string source,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Generic schema importer interface with strongly-typed importer configuration.
/// </summary>
/// <typeparam name="TConfiguration">The importer's strongly-typed configuration type.</typeparam>
public interface ISchemaImporter<TConfiguration> : ISchemaImporter
    where TConfiguration : class
{
    /// <summary>
    /// Imports schema and returns the discovered DataStore configuration.
    /// </summary>
    new Task<IGenericResult<DataStoreConfiguration>> Import(
        string source,
        SchemaImporterOptions? options = null,
        CancellationToken cancellationToken = default);
}
