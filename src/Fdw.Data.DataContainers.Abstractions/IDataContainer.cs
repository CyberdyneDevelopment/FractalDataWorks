using Fdw.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.DataStores.Abstractions;
using Fdw.Results;
using Fdw.Data.DataContainers.Abstractions.ContainerWriteModeOptions;

namespace Fdw.Data.DataContainers.Abstractions;

/// <summary>
/// Represents a data container that defines the physical format and structure of data.
/// DataContainers abstract the format (CSV, JSON, SQL table) from the logical data access.
/// </summary>
/// <remarks>
/// IDataContainer represents WHAT format data is stored in, separate from WHERE it lives
/// (data store) or HOW it's accessed (connections). Examples:
/// - CSV file with specific column structure
/// - JSON document with defined schema
/// - SQL table with columns and types
/// - Parquet file with column definitions
/// - XML document with element structure
/// </remarks>
public interface IDataContainer
{
    /// <summary>
    /// Gets the unique identifier for this data container.
    /// </summary>
    /// <value>A unique identifier for this container instance.</value>
    string Id { get; }

    /// <summary>
    /// Gets the display name for this data container.
    /// </summary>
    /// <value>A human-readable name for UI and logging purposes.</value>
    string Name { get; }

    /// <summary>
    /// Gets the container type (e.g., "CsvFile", "JsonDocument", "SqlTable").
    /// </summary>
    /// <value>The container type identifier for format handling.</value>
    string ContainerType { get; }

    /// <summary>
    /// Gets the schema definition for this container.
    /// </summary>
    /// <value>
    /// The schema that describes the structure, fields, and types within this container.
    /// </value>
    IDataSchema Schema { get; }

    /// <summary>
    /// Gets metadata about this data container.
    /// </summary>
    /// <value>Additional properties and configuration information.</value>
    IReadOnlyDictionary<string, object> Metadata { get; }

    /// <summary>
    /// Gets the data location where this container can be found.
    /// </summary>
    /// <value>
    /// The complete location specification including store, path, and parameters.
    /// </value>
    DataLocation Location { get; }

    /// <summary>
    /// Gets format-specific configuration for this container.
    /// </summary>
    /// <value>
    /// Configuration settings specific to the container format, such as
    /// CSV delimiters, JSON parsing options, or SQL connection settings.
    /// </value>
    IGenericConfiguration Configuration { get; }

    /// <summary>
    /// Validates that this container can be read from the specified location.
    /// </summary>
    /// <param name="location">The location to validate access for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating whether read access is possible.</returns>
    /// <remarks>
    /// This method performs validation without actually reading data.
    /// It checks for accessibility, permissions, schema compatibility, etc.
    /// </remarks>
    Task<IGenericResult> ValidateReadAccess(DataLocation location, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that this container can be written to the specified location.
    /// </summary>
    /// <param name="location">The location to validate write access for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating whether write access is possible.</returns>
    /// <remarks>
    /// This method performs validation without actually writing data.
    /// It checks for write permissions, schema compatibility, format support, etc.
    /// </remarks>
    Task<IGenericResult> ValidateWriteAccess(DataLocation location, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets estimated metrics for reading from this container.
    /// </summary>
    /// <param name="location">The location to estimate metrics for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Estimated read metrics, or failure if unavailable.</returns>
    /// <remarks>
    /// This method provides estimates for query planning and optimization.
    /// Metrics might include record count, data size, read time, etc.
    /// </remarks>
    Task<IGenericResult<ContainerMetrics>> GetReadMetrics(DataLocation location, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a reader instance for accessing data from this container.
    /// </summary>
    /// <param name="location">The location to read from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A reader instance configured for this container format.</returns>
    /// <remarks>
    /// The reader will be configured with the container's schema and format settings.
    /// Callers are responsible for disposing the reader when finished.
    /// </remarks>
    Task<IGenericResult<IDataReader>> CreateReader(DataLocation location, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a writer instance for writing data to this container.
    /// </summary>
    /// <param name="location">The location to write to.</param>
    /// <param name="writeMode">The write mode (overwrite, append, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A writer instance configured for this container format.</returns>
    /// <remarks>
    /// The writer will be configured with the container's schema and format settings.
    /// Callers are responsible for disposing the writer when finished.
    /// </remarks>
    Task<IGenericResult<IDataWriter>> CreateWriter(DataLocation location, IContainerWriteMode? writeMode = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers the actual schema of data at the specified location.
    /// </summary>
    /// <param name="location">The location to discover schema for.</param>
    /// <param name="sampleSize">The number of records to sample for schema inference.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The discovered schema, or failure if schema cannot be determined.</returns>
    /// <remarks>
    /// This method analyzes actual data to determine schema, which may differ
    /// from the container's declared schema. Useful for schema validation and
    /// dynamic schema scenarios.
    /// </remarks>
    Task<IGenericResult<IDataSchema>> DiscoverSchema(DataLocation location, int sampleSize = 1000, CancellationToken cancellationToken = default);
}
