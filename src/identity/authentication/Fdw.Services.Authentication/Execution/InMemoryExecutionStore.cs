using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Execution;
using Fdw.Services.Authentication.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Execution;

/// <summary>
/// Holds suspended flows in process memory.
/// </summary>
/// <remarks>
/// Correct only for a single instance. Behind a load balancer a caller can return to a different
/// node than the one that suspended them, and their login vanishes — so this is for development and
/// for hosts that genuinely run one process, not a default to leave in place.
/// </remarks>
public sealed class InMemoryExecutionStore : IAuthenticationExecutionStore
{
    private readonly ConcurrentDictionary<string, ExecutionRecord> _records = new(StringComparer.Ordinal);
    private readonly ILogger<InMemoryExecutionStore> _logger;

    /// <summary>Initializes a new instance of the <see cref="InMemoryExecutionStore"/> class.</summary>
    /// <param name="logger">The logger.</param>
    public InMemoryExecutionStore(ILogger<InMemoryExecutionStore>? logger = null)
        => _logger = logger ?? NullLogger<InMemoryExecutionStore>.Instance;

    /// <inheritdoc />
    public Task<IGenericResult<string>> Suspend(
        ExecutionRecord record, CancellationToken cancellationToken = default)
    {
        if (record is null)
            return Task.FromResult(GenericResult<string>.Failure(ExecutionStoreLog.RecordMissing(_logger)));

        var token = Base64Url(RandomNumberGenerator.GetBytes(32));

        _records[token] = record;
        ExecutionStoreLog.Suspended(_logger, record.Id, record.FlowName, record.CurrentStepIndex);

        return Task.FromResult(GenericResult<string>.Success(token));
    }

    /// <inheritdoc />
    public Task<IGenericResult<ExecutionRecord>> TryConsume(
        string resumeToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(resumeToken))
            return Task.FromResult(GenericResult<ExecutionRecord>.Failure(
                ExecutionStoreLog.TokenMissing(_logger)));

        if (!_records.TryRemove(resumeToken, out var record))
        {
            return Task.FromResult(GenericResult<ExecutionRecord>.Failure(
                ExecutionStoreLog.NotResumable(_logger)));
        }

        if (record.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            ExecutionStoreLog.Expired(_logger, record.Id);
            return Task.FromResult(GenericResult<ExecutionRecord>.Failure(
                ExecutionStoreLog.NotResumable(_logger)));
        }

        ExecutionStoreLog.Consumed(_logger, record.Id, record.FlowName);
        return Task.FromResult(GenericResult<ExecutionRecord>.Success(record));
    }

    private static string Base64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
