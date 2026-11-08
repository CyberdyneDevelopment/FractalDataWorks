using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// A stand-in for the store of disabled states: every connection is enabled until a test names it disabled, or a test
/// makes the read fail.
/// </summary>
internal sealed class DisabledStatesDouble : IConnectionDisabledStates
{
    private readonly Dictionary<Guid, ConnectionDisabledRecord> _disabled = [];
    private IGenericResult? _readFailure;

    public List<Guid> Reads { get; } = [];

    public DisabledStatesDouble Disabled(Guid connectionId, string reason, DateTimeOffset? at = null)
    {
        _disabled[connectionId] = new ConnectionDisabledRecord(connectionId, at ?? new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero), reason, "a test");
        return this;
    }

    public DisabledStatesDouble ReadFails(IGenericResult failure)
    {
        _readFailure = failure;
        return this;
    }

    public Task<IGenericResult<ConnectionDisabledRecord?>> Read(Guid connectionId, CancellationToken cancellationToken = default)
    {
        Reads.Add(connectionId);
        return Task.FromResult(_readFailure is { } failure
            ? failure.ToNewResult<ConnectionDisabledRecord?>()
            : GenericResult<ConnectionDisabledRecord?>.Success(_disabled.TryGetValue(connectionId, out var record) ? record : null));
    }

    public Task<IGenericResult<IReadOnlyDictionary<Guid, ConnectionDisabledRecord>>> ReadMany(IReadOnlyCollection<Guid> connectionIds, CancellationToken cancellationToken = default)
    {
        Reads.AddRange(connectionIds);
        return Task.FromResult(_readFailure is { } failure
            ? failure.ToNewResult<IReadOnlyDictionary<Guid, ConnectionDisabledRecord>>()
            : GenericResult<IReadOnlyDictionary<Guid, ConnectionDisabledRecord>>.Success(
                connectionIds.Where(_disabled.ContainsKey).ToDictionary(id => id, id => _disabled[id])));
    }

    public Task<IGenericResult<ConnectionDisabledRecord>> Disable(
        Guid connectionId, Guid requestingTenantId, bool requesterMayDisableOtherTenants, string reason, string disabledBy, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("The provider tests only read the state.");

    public Task<IGenericResult> Enable(Guid connectionId, string reason, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("The provider tests only read the state.");
}
