using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Represents a dataset definition with schema information and query capabilities.
/// Datasets define the structure and metadata for collections of typed data records.
/// </summary>
public interface IDataSetType : ITypeOption<int, DataSetTypeBase>
{
    /// <summary>
    /// Gets the detailed description of this dataset.
    /// </summary>
    /// <value>A comprehensive description explaining the purpose and content of this dataset.</value>
    string Description { get; }

    /// <summary>
    /// Gets the .NET type of the record/entity that this dataset represents.
    /// </summary>
    /// <value>The System.Type of the data record or entity class.</value>
    Type RecordType { get; }

    /// <summary>
    /// Gets the collection of fields that define the schema of this dataset.
    /// </summary>
    /// <value>A collection of field metadata describing the structure of records in this dataset.</value>
    IReadOnlyCollection<IDataField> Fields { get; }

    /// <summary>
    /// Gets the names of fields that form the primary key for this dataset.
    /// </summary>
    /// <value>A collection of field names that uniquely identify records in this dataset.</value>
    IReadOnlyCollection<string> KeyFields { get; }

    /// <summary>
    /// Gets the configuration section name where this dataset's settings are stored.
    /// </summary>
    /// <value>The configuration section path for dataset-specific settings.</value>
    string ConfigurationSection { get; }

    /// <summary>
    /// Gets the version of this dataset schema.
    /// </summary>
    /// <value>The schema version string for compatibility and migration purposes.</value>
    string Version { get; }

    /// <summary>
    /// Executes a data command against an instance of this dataset type.
    /// </summary>
    /// <typeparam name="T">The expected result type.</typeparam>
    /// <param name="context">The execution context carrying the resolved dataset configuration and the
    /// providers the strategy needs to pull and join sources. The strategy reads the instance's
    /// structure (sources, joins, fields) from <see cref="IDataSetExecutionContext.Configuration"/>.</param>
    /// <param name="command">The data command to execute.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the typed execution outcome.</returns>
    Task<IGenericResult<T>> Execute<T>(IDataSetExecutionContext context, IDataCommand command, CancellationToken ct = default);
}

