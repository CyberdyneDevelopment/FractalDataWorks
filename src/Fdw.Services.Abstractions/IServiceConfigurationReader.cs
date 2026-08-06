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
// Why: T is invariant — Task{TResult} is invariant in TResult; covariance on an async-returning
// interface is impossible in C#. Use ConfigurationReaderAdapter{TBase,TDerived} at the storage
// layer when polymorphic registration needs base-typed slots.
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
