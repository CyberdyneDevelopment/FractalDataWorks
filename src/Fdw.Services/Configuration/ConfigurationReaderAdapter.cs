using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Configuration;

/// <summary>
/// Adapts <see cref="IServiceConfigurationReader{TDerived}"/> to
/// <see cref="IServiceConfigurationReader{TBase}"/> when the inner reader's
/// TDerived is a subtype of TBase.
/// </summary>
/// <remarks>
/// Used when a provider dictionary stores base-typed readers but a concrete reader is typed to a
/// derived configuration (e.g. ConnectionConfigurationProvider stores
/// IServiceConfigurationReader{ConnectionConfiguration} but the registered reader is
/// DefaultConfigurationProvider{MsSqlConnectionConfiguration,...}).
/// </remarks>
// Why: Task{T} is invariant in T, so IServiceConfigurationReader{out TConfig} is illegal in C#.
// This adapter handles the upcast at the storage boundary — each Get overload awaits the inner
// reader and returns the result cast to TBase, which is safe because TDerived : TBase.
public sealed class ConfigurationReaderAdapter<TBase, TDerived> : IServiceConfigurationReader<TBase>
    where TBase : class, IGenericConfiguration
    where TDerived : class, TBase
{
    private readonly IServiceConfigurationReader<TDerived> _inner;

    /// <summary>Wraps a derived-typed reader as a base-typed reader.</summary>
    public ConfigurationReaderAdapter(IServiceConfigurationReader<TDerived> inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<TBase>> Get(string name, CancellationToken ct = default)
    {
        var result = await _inner.Get(name, ct).ConfigureAwait(false);
        if (!result.IsSuccess) return result.ToNewResult<TBase>();
        return result.ToNewResult<TBase>(result.Value!);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<TBase>> Get(Guid id, CancellationToken ct = default)
    {
        var result = await _inner.Get(id, ct).ConfigureAwait(false);
        if (!result.IsSuccess) return result.ToNewResult<TBase>();
        return result.ToNewResult<TBase>(result.Value!);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<TBase>>> Get(CancellationToken ct = default)
    {
        var result = await _inner.Get(ct).ConfigureAwait(false);
        if (!result.IsSuccess) return result.ToNewResult<IReadOnlyList<TBase>>();
        // Why: IReadOnlyList{out T} is covariant, so this cast is safe.
        return GenericResult<IReadOnlyList<TBase>>.Success((IReadOnlyList<TBase>)result.Value!);
    }
}
