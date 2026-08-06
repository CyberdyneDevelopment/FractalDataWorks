using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Service for managing DataSet field schemas.
/// </summary>
public interface IDataSetSchemaService
{
    /// <summary>Gets the schema (field definitions) for a DataSet.</summary>
    Task<IGenericResult<IReadOnlyList<DataSetFieldDefinition>>> GetSchema(
        Guid dataSetId, CancellationToken cancellationToken = default);

    /// <summary>Validates that a physical DataSet conforms to an abstract DataSet schema.</summary>
    Task<IGenericResult> ValidateConformance(
        Guid physicalDataSetId, Guid abstractDataSetId,
        CancellationToken cancellationToken = default);

    /// <summary>Saves the field schema for a DataSet.</summary>
    Task<IGenericResult> SaveSchema(
        Guid dataSetId,
        IReadOnlyList<DataSetFieldDefinition> fields,
        CancellationToken cancellationToken = default);
}
