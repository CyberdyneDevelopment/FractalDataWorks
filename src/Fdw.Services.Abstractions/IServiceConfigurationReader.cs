using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;

namespace Fdw.Services.Abstractions;

/// <summary>
/// Read-only view of a service configuration provider.
/// Provides Get overloads for name, id, and all-items queries.
/// </summary>
/// <typeparam name="TConfig">The configuration type.</typeparam>
public interface IServiceConfigurationReader<TConfig>
    where TConfig : class, IGenericConfiguration
{
    /// <summary>Gets a configuration by name.</summary>
    Task<IGenericResult<TConfig>> Get(string name, CancellationToken ct = default);

    /// <summary>Gets a configuration by ID.</summary>
    Task<IGenericResult<TConfig>> Get(Guid id, CancellationToken ct = default);

    /// <summary>Gets all configurations.</summary>
    Task<IGenericResult<IReadOnlyList<TConfig>>> Get(CancellationToken ct = default);
}
