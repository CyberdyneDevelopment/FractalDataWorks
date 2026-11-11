using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Caching;

/// <summary>
/// Publishes the invalidation tags a configuration write changed to the other hosts, by writing a new token for each
/// tag into the cache change token container (FDW-4782).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Publish"/> never blocks and never fails the write that calls it: the tags are queued, and a background loop
/// writes them out. Tags queued while a flush runs are coalesced into the next, so a burst of writes to one container
/// is one token write. Each tag is an Update filtered on the tag; when it matched no row, an Insert; when the Insert
/// fails (another host inserted the same tag first), the Update is tried once more. Only Query, Update and Insert are
/// used, so every connection type serves it.
/// </para>
/// <para>
/// A change that cannot be published is logged with its reason and dropped: the other hosts then show the old value
/// until their cache entries expire, which is the bound this has without it. A database that cannot be reached at all
/// is <see cref="DatabaseResetWatcher"/>'s, which empties every cache.
/// </para>
/// <para>
/// The loop starts with the first publish and runs for the life of the process, outside the flow of the write that
/// started it: it carries no caller's context, and writes under the system context, since a token row belongs to no tenant.
/// </para>
/// <para>
/// It holds nothing that needs disposing, on purpose: the gateway a host builds while it registers comes from a provider
/// that is disposed when registration ends, and that gateway keeps publishing for the life of the process.
/// </para>
/// </remarks>
public sealed class CacheChangePublisher : ICacheChangePublisher
{
    private readonly Func<IGenericResult<IConfigurationGateway>> _gateway;
    private readonly IAuthenticationContextAccessor _authenticationContextAccessor;
    private readonly DataStoreTarget _target;
    private readonly string _ownTag;
    private readonly ILogger<CacheChangePublisher> _logger;

    private readonly Lock _gate = new();

    // What the one loop does next, in the order asked: a null is a wake-up from Publish, a request is a FlushPending
    // caller waiting on its completion. One reader, so flushes run one at a time and in order.
    private readonly Channel<(TaskCompletionSource<int> Completion, CancellationToken Token)?> _requests =
        Channel.CreateUnbounded<(TaskCompletionSource<int> Completion, CancellationToken Token)?>(
            new UnboundedChannelOptions { SingleReader = true });

    private HashSet<string> _pending = new(StringComparer.OrdinalIgnoreCase);

    // 1 while a wake-up is waiting in _requests: however many publishes come while the loop is busy, one wake-up.
    private int _wakePending;
    private int _started;

    /// <summary>Initializes a new instance of the <see cref="CacheChangePublisher"/> class.</summary>
    /// <param name="gateway">Gives the gateway onto the store the tokens live in, asked at each flush.</param>
    /// <param name="authenticationContextAccessor">The accessor the system context is set on for the token writes.</param>
    /// <param name="target">The container the tokens live in.</param>
    /// <param name="logger">Optional logger.</param>
    public CacheChangePublisher(
        Func<IGenericResult<IConfigurationGateway>> gateway,
        IAuthenticationContextAccessor authenticationContextAccessor,
        DataStoreTarget target,
        ILogger<CacheChangePublisher>? logger = null)
    {
        _gateway = gateway;
        _authenticationContextAccessor = authenticationContextAccessor;
        _target = target;
        _ownTag = CacheKeyBuilder.TagFor(target);
        _logger = logger ?? NullLogger<CacheChangePublisher>.Instance;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The token container's own tag is never published: writing a token is itself a write through the gateway, and
    /// publishing it would write another.
    /// </remarks>
    public void Publish(IReadOnlyList<string> tags)
    {
        var queued = false;
        lock (_gate)
        {
            for (var i = 0; i < tags.Count; i++)
            {
                if (!string.Equals(tags[i], _ownTag, StringComparison.OrdinalIgnoreCase) && _pending.Add(tags[i]))
                    queued = true;
            }
        }

        if (!queued)
            return;

        Wake();
        EnsureStarted();
    }

    /// <summary>
    /// Writes out every tag queued so far. The background loop does this after each publish; a caller that has to know
    /// the changes it queued are published (a test, a host shutting down) can too. Flushes run one at a time and in the
    /// order asked, so when this returns, every tag queued before it was called has been written or logged as not written.
    /// </summary>
    /// <param name="cancellationToken">
    /// Cancels this caller's flush only. The tags it had taken go back in the queue for the next flush, so a caller that
    /// gives up never drops changes other writers queued.
    /// </param>
    /// <returns>How many tags this flush published.</returns>
    public async Task<int> FlushPending(CancellationToken cancellationToken = default)
    {
        var completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = _requests.Writer.TryWrite((completion, cancellationToken));
        EnsureStarted();
        return await completion.Task.ConfigureAwait(false);
    }

    private void Wake()
    {
        if (Interlocked.Exchange(ref _wakePending, 1) == 0)
            _ = _requests.Writer.TryWrite(null);
    }

    private void EnsureStarted()
    {
        if (Interlocked.CompareExchange(ref _started, 1, 0) != 0)
            return;

        // Outside the caller's flow: the loop outlives the request that started it and must not carry its context.
        using (ExecutionContext.SuppressFlow())
            _ = Task.Run(Run);
    }

    private async Task Run()
    {
        await foreach (var request in _requests.Reader.ReadAllAsync(CancellationToken.None).ConfigureAwait(false))
        {
            if (request is not { } asked)
            {
                // Cleared before the batch is taken, so a publish from here on wakes the loop again.
                _ = Interlocked.Exchange(ref _wakePending, 0);
                _ = await Flush(CancellationToken.None).ConfigureAwait(false);
                continue;
            }

            try
            {
                _ = asked.Completion.TrySetResult(await Flush(asked.Token).ConfigureAwait(false));
            }
            catch (OperationCanceledException ex)
            {
                // The cancellation the flush observed: the asker's own token, or one a store call gave (FDW-5189).
                _ = asked.Completion.TrySetCanceled(ex.CancellationToken);
            }
            catch (Exception ex)
            {
                _ = asked.Completion.TrySetException(ex);
            }
        }
    }

    // Takes everything queued by now and writes it.
    private async Task<int> Flush(CancellationToken cancellationToken)
    {
        HashSet<string> batch;
        lock (_gate)
        {
            batch = _pending;
            _pending = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        if (batch.Count == 0)
            return 0;

        try
        {
            return await Write(batch, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Back in the queue, and the loop woken for it: the tags belong to the writers that queued them, not to
            // the caller that cancelled. A tag already written before the cancel is written again, which is harmless.
            lock (_gate)
            {
                _pending.UnionWith(batch);
            }

            Wake();
            throw;
        }
    }

    private async Task<int> Write(HashSet<string> batch, CancellationToken cancellationToken)
    {
        var tags = string.Join(", ", batch);
        try
        {
            var gateway = _gateway();
            if (!gateway.IsSuccess || gateway.Value is not { } store)
            {
                CacheChangeLog.GatewayUnavailable(_logger, _target.DataStore, tags, gateway.NoValueReason().Message);
                return 0;
            }

            using var system = new SystemAuthenticationContextScope(_authenticationContextAccessor);
            var published = new List<string>(batch.Count);
            foreach (var tag in batch)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (await WriteToken(store, tag, cancellationToken).ConfigureAwait(false))
                    published.Add(tag);
            }

            if (published.Count > 0)
                CacheChangeLog.Published(_logger, published.Count, string.Join(", ", published));

            return published.Count;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            CacheChangeLog.FlushThrew(_logger, ex, tags);
            return 0;
        }
    }

    // Update, then Insert when no row matched, then the Update once more when the Insert lost a race with another host.
    private async Task<bool> WriteToken(IConfigurationGateway store, string tag, CancellationToken cancellationToken)
    {
        var row = new CacheChangeToken { Tag = tag, Token = Guid.CreateVersion7(), ChangedAt = DateTimeOffset.UtcNow };

        var updated = await Update(store, row, cancellationToken).ConfigureAwait(false);
        if (!updated.IsSuccess)
        {
            CacheChangeLog.UpdateFailed(_logger, tag, updated.FailureReason().Message);
            return false;
        }

        if (updated.Value > 0)
            return true;

        var inserted = await store.Execute<int>(new InsertCommand<CacheChangeToken>(row), _target, cancellationToken).ConfigureAwait(false);
        if (inserted.IsSuccess)
            return true;

        var retried = await Update(store, row, cancellationToken).ConfigureAwait(false);
        if (!retried.IsSuccess)
        {
            CacheChangeLog.InsertAndRetryFailed(_logger, tag, inserted.FailureReason().Message, retried.FailureReason().Message);
            return false;
        }

        if (retried.Value > 0)
            return true;

        CacheChangeLog.InsertFailedNoRow(_logger, tag, inserted.FailureReason().Message);
        return false;
    }

    private Task<IGenericResult<int>> Update(IConfigurationGateway store, CacheChangeToken row, CancellationToken cancellationToken)
        => store.Execute<int>(
            new UpdateCommand<CacheChangeToken>(row)
            {
                Filter = new FilterExpression
                {
                    Root = new FilterCondition { PropertyName = nameof(CacheChangeToken.Tag), Operator = FilterOperators.ByName("Equal"), Value = row.Tag },
                },
            },
            _target,
            cancellationToken);
}
