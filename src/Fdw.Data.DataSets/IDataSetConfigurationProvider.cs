using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Provides centralized registry and resolution for logical DataSet <em>configurations</em>.
/// Returns <see cref="DataSetConfiguration"/> records — not runtime services.
/// Use <c>IDataSetProvider</c> (in <c>Fdw.Services.Data.Abstractions</c>) when you need the live <see cref="Fdw.Data.Abstractions.IDataSet"/> runtime.
/// </summary>
/// <remarks>
/// Merges three configuration sources in priority order:
/// <list type="number">
/// <item><description>IOptionsMonitor (ctrl/system DataSets from configurationSchema.json)</description></item>
/// <item><description>ConfigurationDb (user-defined DataSets in the cfg schema)</description></item>
/// <item><description>DataSetTypes TypeCollection (code-defined static DataSets)</description></item>
/// </list>
/// </remarks>
public interface IDataSetConfigurationProvider
{
    /// <summary>
    /// Gets a DataSet configuration by name.
    /// </summary>
    /// <param name="name">The unique name of the DataSet.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the <see cref="DataSetConfiguration"/> if found, or failure.</returns>
    Task<IGenericResult<DataSetConfiguration>> Get(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a DataSet configuration by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the DataSet.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the <see cref="DataSetConfiguration"/> if found, or failure.</returns>
    Task<IGenericResult<DataSetConfiguration>> Get(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all DataSet configurations, merging all three sources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing all registered <see cref="DataSetConfiguration"/> records.</returns>
    Task<IGenericResult<IReadOnlyList<DataSetConfiguration>>> Get(CancellationToken cancellationToken = default);
}
