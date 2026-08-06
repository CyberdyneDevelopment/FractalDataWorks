using Fdw.Data.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Provides live <see cref="IDataSet"/> runtime instances resolved by name or ID.
/// </summary>
/// <remarks>
/// This is the <em>service</em> provider — it returns the live runtime, not a configuration record.
/// For configuration records, use <c>IDataSetConfigurationProvider</c> (defined in
/// <c>Fdw.Data.DataSets.Abstractions</c>).
/// Internally calls <c>IDataSetConfigurationProvider</c> to load the configuration,
/// then <c>IDataSetFactory</c> to build the live <see cref="IDataSet"/> runtime.
/// </remarks>
public interface IDataSetProvider
{
    /// <summary>
    /// Gets a live <see cref="IDataSet"/> runtime by name.
    /// </summary>
    /// <param name="name">The unique name of the DataSet.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the live <see cref="IDataSet"/>, or failure.</returns>
    Task<IGenericResult<IDataSet>> Get(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a live <see cref="IDataSet"/> runtime by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the DataSet.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the live <see cref="IDataSet"/>, or failure.</returns>
    Task<IGenericResult<IDataSet>> Get(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all live <see cref="IDataSet"/> runtimes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing all registered <see cref="IDataSet"/> instances.</returns>
    Task<IGenericResult<IReadOnlyList<IDataSet>>> Get(CancellationToken cancellationToken = default);
}
