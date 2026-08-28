using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Quality.Configuration;
using Fdw.Services.Quality.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Quality.Services;

/// <summary>
/// Implementation of environment promotion operations.
/// </summary>
public sealed class PromotionService : IPromotionService
{
    private readonly ILogger _logger;
    private readonly QualityConfigurationProvider _qualityProvider;
    private readonly List<PromotionRequestConfiguration> _inMemoryRequests = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PromotionService"/> class.
    /// </summary>
    public PromotionService(
        ILoggerFactory loggerFactory,
        QualityConfigurationProvider qualityProvider)
    {
        _logger = loggerFactory.CreateLogger<PromotionService>();
        _qualityProvider = qualityProvider;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<EnvironmentConfiguration>>> GetEnvironments(CancellationToken ct = default)
    {
        var result = await _qualityProvider.GetAllEnvironments(ct).ConfigureAwait(false);
        if (!result.IsSuccess) return result;
        var environments = (result.Value ?? []).OrderBy(e => e.PromotionOrder).ToList();
        return GenericResult<IReadOnlyList<EnvironmentConfiguration>>.Success(environments);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<PromotionRequestConfiguration>> CreateRequest(PromotionRequestConfiguration request, CancellationToken ct = default)
    {
        try
        {
            if (request.SourceEnvironment.Equals(request.TargetEnvironment, StringComparison.OrdinalIgnoreCase))
            {
                return GenericResult<PromotionRequestConfiguration>.Failure(
                    PromotionLog.SameEnvironmentError(_logger, request.SourceEnvironment));
            }

            var sourceLookup = await _qualityProvider.GetEnvironment(request.SourceEnvironment, ct).ConfigureAwait(false);
            if (!sourceLookup.IsSuccess || sourceLookup.Value is null)
            {
                return GenericResult<PromotionRequestConfiguration>.Failure(
                    PromotionLog.EnvironmentNotFound(_logger, request.SourceEnvironment));
            }

            var targetLookup = await _qualityProvider.GetEnvironment(request.TargetEnvironment, ct).ConfigureAwait(false);
            if (!targetLookup.IsSuccess || targetLookup.Value is null)
            {
                return GenericResult<PromotionRequestConfiguration>.Failure(
                    PromotionLog.EnvironmentNotFound(_logger, request.TargetEnvironment));
            }

            request.Id = request.Id == Guid.Empty ? Guid.NewGuid() : request.Id;
            request.Status = "Pending";
            request.CreatedAt = DateTimeOffset.UtcNow;
            _inMemoryRequests.Add(request);

            PromotionLog.RequestCreated(_logger, request.SourceEnvironment, request.TargetEnvironment, request.RequestedBy);
            return GenericResult<PromotionRequestConfiguration>.Success(request);
        }
        catch (Exception ex)
        {
            return GenericResult<PromotionRequestConfiguration>.Failure(
                PromotionLog.PromotionFailed(_logger, ex, request.Id));
        }
    }

    /// <inheritdoc/>
    public Task<IGenericResult<PromotionRequestConfiguration>> GetRequest(Guid requestId, CancellationToken ct = default)
    {
        PromotionLog.LoadingRequest(_logger, requestId);

        var request = _inMemoryRequests.FirstOrDefault(r => r.Id == requestId);
        if (request == null)
        {
            return Task.FromResult(GenericResult<PromotionRequestConfiguration>.Failure(
                PromotionLog.RequestNotFound(_logger, requestId)));
        }

        return Task.FromResult(GenericResult<PromotionRequestConfiguration>.Success(request));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<IReadOnlyList<PromotionRequestConfiguration>>> GetRequests(string? status, CancellationToken ct = default)
    {
        var requests = _inMemoryRequests.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            requests = requests.Where(r => r.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        var list = requests.OrderByDescending(r => r.CreatedAt).ToList();
        return Task.FromResult(GenericResult<IReadOnlyList<PromotionRequestConfiguration>>.Success(list));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<PromotionRequestConfiguration>> ApproveRequest(Guid requestId, string approvedBy, CancellationToken ct = default)
    {
        var request = _inMemoryRequests.FirstOrDefault(r => r.Id == requestId);
        if (request == null)
        {
            return Task.FromResult(GenericResult<PromotionRequestConfiguration>.Failure(
                PromotionLog.RequestNotFound(_logger, requestId)));
        }

        request.Status = "Approved";
        request.ApprovedBy = approvedBy;
        request.ApprovedAt = DateTimeOffset.UtcNow;

        PromotionLog.RequestApproved(_logger, requestId, approvedBy);
        return Task.FromResult(GenericResult<PromotionRequestConfiguration>.Success(request));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<PromotionRequestConfiguration>> RejectRequest(Guid requestId, string rejectedBy, string reason, CancellationToken ct = default)
    {
        var request = _inMemoryRequests.FirstOrDefault(r => r.Id == requestId);
        if (request == null)
        {
            return Task.FromResult(GenericResult<PromotionRequestConfiguration>.Failure(
                PromotionLog.RequestNotFound(_logger, requestId)));
        }

        request.Status = "Rejected";
        request.Notes = reason;

        PromotionLog.RequestRejected(_logger, requestId, rejectedBy, reason);
        return Task.FromResult(GenericResult<PromotionRequestConfiguration>.Success(request));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<PromotionResult>> ExecutePromotion(Guid requestId, CancellationToken ct = default)
    {
        try
        {
            var request = _inMemoryRequests.FirstOrDefault(r => r.Id == requestId);
            if (request == null)
            {
                return Task.FromResult(GenericResult<PromotionResult>.Failure(
                    PromotionLog.RequestNotFound(_logger, requestId)));
            }

            if (!request.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(GenericResult<PromotionResult>.Failure(
                    PromotionLog.NotApproved(_logger, requestId, request.Status)));
            }

            // Parse items from JSON (simulated - in production would deserialize)
            var itemCount = 0;
            PromotionLog.PromotionStarted(_logger, requestId, itemCount);

            // Simulate promotion execution
            var items = new List<PromotionItemResult>();
            var completedAt = DateTimeOffset.UtcNow;

            var result = new PromotionResult(
                RequestId: requestId,
                SourceEnvironment: request.SourceEnvironment,
                TargetEnvironment: request.TargetEnvironment,
                TotalItems: items.Count,
                SuccessfulItems: items.Count(i => i.Success),
                FailedItems: items.Count(i => !i.Success),
                CompletedAt: completedAt,
                Items: items);

            request.Status = "Completed";
            request.CompletedAt = completedAt;

            PromotionLog.PromotionCompleted(_logger, requestId, result.SuccessfulItems);
            return Task.FromResult(GenericResult<PromotionResult>.Success(result));
        }
        catch (Exception ex)
        {
            return Task.FromResult(GenericResult<PromotionResult>.Failure(
                PromotionLog.PromotionFailed(_logger, ex, requestId)));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<ConfigDiff>> CompareEnvironments(string sourceEnvironment, string targetEnvironment, string entityType, string entityName, CancellationToken ct = default)
    {
        try
        {
            PromotionLog.ComparingEnvironments(_logger, sourceEnvironment, targetEnvironment);

            var sourceLookup = await _qualityProvider.GetEnvironment(sourceEnvironment, ct).ConfigureAwait(false);
            if (!sourceLookup.IsSuccess || sourceLookup.Value is null)
            {
                return GenericResult<ConfigDiff>.Failure(
                    PromotionLog.EnvironmentNotFound(_logger, sourceEnvironment));
            }

            var targetLookup = await _qualityProvider.GetEnvironment(targetEnvironment, ct).ConfigureAwait(false);
            if (!targetLookup.IsSuccess || targetLookup.Value is null)
            {
                return GenericResult<ConfigDiff>.Failure(
                    PromotionLog.EnvironmentNotFound(_logger, targetEnvironment));
            }

            // Simulated comparison - in production would load and compare actual configurations
            var differences = new List<ConfigDiffItem>();

            var diff = new ConfigDiff(
                SourceEnvironment: sourceEnvironment,
                TargetEnvironment: targetEnvironment,
                EntityType: entityType,
                EntityName: entityName,
                Differences: differences);

            var addedCount = differences.Count(d => string.Equals(d.DiffType, "Added", StringComparison.Ordinal));
            var modifiedCount = differences.Count(d => string.Equals(d.DiffType, "Modified", StringComparison.Ordinal));
            var removedCount = differences.Count(d => string.Equals(d.DiffType, "Removed", StringComparison.Ordinal));
            PromotionLog.ComparisonCompleted(_logger, addedCount, modifiedCount, removedCount);

            return GenericResult<ConfigDiff>.Success(diff);
        }
        catch (Exception ex)
        {
            return GenericResult<ConfigDiff>.Failure(
                PromotionLog.PromotionFailed(_logger, ex, Guid.Empty));
        }
    }
}
