using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Quality.Configuration;
using Fdw.Services.Quality.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Quality.Services;

/// <summary>
/// Implementation of quality validation rule management and execution.
/// </summary>
public sealed class QualityService : IQualityService
{
    private readonly ILogger _logger;
    private readonly IOptionsMonitor<List<QualityRuleConfiguration>> _rulesMonitor;
    private readonly List<QualityRuleConfiguration> _inMemoryRules = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="QualityService"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="rulesMonitor">The rules configuration monitor.</param>
    public QualityService(
        ILoggerFactory loggerFactory,
        IOptionsMonitor<List<QualityRuleConfiguration>> rulesMonitor)
    {
        _logger = loggerFactory.CreateLogger<QualityService>();
        _rulesMonitor = rulesMonitor;
    }

    /// <inheritdoc/>
    public Task<IGenericResult<QualityRuleConfiguration>> CreateRule(QualityRuleConfiguration rule, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(rule.RuleType))
            {
                return Task.FromResult(GenericResult<QualityRuleConfiguration>.Failure(
                    QualityLog.InvalidRuleType(_logger, rule.RuleType ?? string.Empty)));
            }

            rule.Id = rule.Id == Guid.Empty ? Guid.NewGuid() : rule.Id;
            _inMemoryRules.Add(rule);

            QualityLog.RuleCreated(_logger, rule.RuleType, rule.DataSetName);
            return Task.FromResult(GenericResult<QualityRuleConfiguration>.Success(rule));
        }
        catch (Exception ex)
        {
            return Task.FromResult(GenericResult<QualityRuleConfiguration>.Failure(
                QualityLog.RuleSaveFailed(_logger, ex, rule.RuleType)));
        }
    }

    /// <inheritdoc/>
    public Task<IGenericResult<QualityRuleConfiguration>> UpdateRule(Guid id, QualityRuleConfiguration rule, CancellationToken ct = default)
    {
        var existing = _inMemoryRules.FirstOrDefault(r => r.Id == id);
        if (existing == null)
        {
            return Task.FromResult(GenericResult<QualityRuleConfiguration>.Failure(
                QualityLog.RuleNotFound(_logger, id)));
        }

        _inMemoryRules.Remove(existing);
        rule.Id = id;
        _inMemoryRules.Add(rule);

        QualityLog.RuleUpdated(_logger, rule.RuleType);
        return Task.FromResult(GenericResult<QualityRuleConfiguration>.Success(rule));
    }

    /// <inheritdoc/>
    public Task<IGenericResult> DeleteRule(Guid id, CancellationToken ct = default)
    {
        var existing = _inMemoryRules.FirstOrDefault(r => r.Id == id);
        if (existing == null)
        {
            return Task.FromResult(GenericResult.Failure(
                QualityLog.RuleNotFound(_logger, id)));
        }

        _inMemoryRules.Remove(existing);
        QualityLog.RuleDeleted(_logger, existing.RuleType);
        return Task.FromResult(GenericResult.Success());
    }

    /// <inheritdoc/>
    public Task<IGenericResult<QualityRuleConfiguration>> GetRule(Guid id, CancellationToken ct = default)
    {
        var rule = _inMemoryRules.FirstOrDefault(r => r.Id == id);
        if (rule == null)
        {
            return Task.FromResult(GenericResult<QualityRuleConfiguration>.Failure(
                QualityLog.RuleNotFound(_logger, id)));
        }

        return Task.FromResult(GenericResult<QualityRuleConfiguration>.Success(rule));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<IReadOnlyList<QualityRuleConfiguration>>> GetRulesForDataSet(string dataSetName, CancellationToken ct = default)
    {
        QualityLog.LoadingRules(_logger, dataSetName);
        var rules = _inMemoryRules.Where(r => string.Equals(r.DataSetName, dataSetName, StringComparison.Ordinal)).ToList();
        QualityLog.RulesLoaded(_logger, rules.Count, dataSetName);
        return Task.FromResult(GenericResult<IReadOnlyList<QualityRuleConfiguration>>.Success(rules));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<IReadOnlyList<QualityRuleConfiguration>>> GetAllRules(CancellationToken ct = default)
    {
        return Task.FromResult(GenericResult<IReadOnlyList<QualityRuleConfiguration>>.Success(_inMemoryRules.ToList()));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<QualityCheckResult>> ExecuteCheck(Guid ruleId, CancellationToken ct = default)
    {
        var rule = _inMemoryRules.FirstOrDefault(r => r.Id == ruleId);
        if (rule == null)
        {
            return Task.FromResult(GenericResult<QualityCheckResult>.Failure(
                QualityLog.RuleNotFound(_logger, ruleId)));
        }

        QualityLog.ExecutingRule(_logger, rule.RuleType, rule.RuleType);

        // Simulated check result - in production, this would execute against real data
        var result = new QualityCheckResult(
            RuleId: rule.Id,
            RuleName: rule.RuleType,
            RuleType: rule.RuleType,
            Passed: true,
            TotalRecords: 100,
            PassedRecords: 100,
            FailedRecords: 0,
            PassRate: 1.0,
            ExecutedAt: DateTimeOffset.UtcNow,
            SampleViolations: Array.Empty<QualityViolation>());

        QualityLog.RulePassed(_logger, rule.RuleType, result.PassedRecords, result.TotalRecords, result.PassRate);
        return Task.FromResult(GenericResult<QualityCheckResult>.Success(result));
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<QualityCheckResult>>> ExecuteAllChecks(string dataSetName, CancellationToken ct = default)
    {
        var rulesResult = await GetRulesForDataSet(dataSetName, ct).ConfigureAwait(false);
        if (rulesResult.IsFailure)
        {
            return rulesResult.ToNewResult<IReadOnlyList<QualityCheckResult>>();
        }

        var rules = rulesResult.Value ?? new List<QualityRuleConfiguration>();
        QualityLog.CheckStarted(_logger, dataSetName, rules.Count);

        var results = new List<QualityCheckResult>();
        foreach (var rule in rules.Where(r => r.IsEnabled))
        {
            var checkResult = await ExecuteCheck(rule.Id, ct).ConfigureAwait(false);
            if (!checkResult.IsSuccess)
            {
                return checkResult.ToNewResult<IReadOnlyList<QualityCheckResult>>();
            }

            if (checkResult.Value is { } result)
            {
                results.Add(result);
            }
            else
            {
                QualityLog.RuleExecutionFailed(_logger, rule.RuleType, checkResult.CurrentMessage ?? string.Empty);
            }
        }

        var passedCount = results.Count(r => r.Passed);
        QualityLog.CheckCompleted(_logger, dataSetName, passedCount, results.Count);

        return GenericResult<IReadOnlyList<QualityCheckResult>>.Success(results);
    }
}
