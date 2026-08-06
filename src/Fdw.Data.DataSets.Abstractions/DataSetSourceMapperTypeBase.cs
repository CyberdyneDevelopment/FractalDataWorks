using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Base class for data set source mapper type definitions.
/// Mappers extract raw records from structured payloads and return dictionaries of string? values.
/// Type coercion is handled by the downstream transform chain, not the mapper.
/// </summary>
public abstract class DataSetSourceMapperTypeBase : TypeOptionBase<int, DataSetSourceMapperTypeBase>, IDataSetSourceMapperType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetSourceMapperTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this mapper type.</param>
    /// <param name="name">The name of this mapper type.</param>
    /// <param name="displayName">The display name for this mapper type.</param>
    /// <param name="description">A description of what this mapper does.</param>
    /// <param name="category">The category for this mapper type (defaults to "Mapper").</param>
    protected DataSetSourceMapperTypeBase(
        int id,
        string name,
        string displayName,
        string description,
        string? category = null)
        : base(id, name, $"Mappers:{name}", displayName, description, category ?? "Mapper")
    {
    }

    /// <summary>
    /// Extracts records from a structured payload using the configured record selector and field mappings.
    /// All extracted values are raw string? — type coercion is the transform chain's responsibility.
    /// </summary>
    /// <param name="context">The mapper context containing payload, record selector, and field mappings.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>
    /// A result containing a list of dictionaries, each mapping logical field names to raw extracted values.
    /// Returns Failure if the payload cannot be parsed or the record selector is invalid.
    /// </returns>
    public abstract Task<IGenericResult<IReadOnlyList<Dictionary<string, object?>>>> MapRecords(
        DataSetSourceMapperContext context,
        CancellationToken cancellationToken = default);
}
