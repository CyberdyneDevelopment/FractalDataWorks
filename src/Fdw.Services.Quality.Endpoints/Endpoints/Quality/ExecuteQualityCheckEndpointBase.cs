using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Quality.Endpoints;


/// <summary>Endpoint that executes a single quality check by rule identifier.</summary>
public abstract class ExecuteQualityCheckEndpointBase : Endpoint<QualityRuleIdRequest, QualityCheckResultResponse>
{
    private readonly QualityConfigurationProvider _provider;
    private readonly IDataGateway _dataGateway;

    /// <summary>Initializes a new instance of the <see cref="ExecuteQualityCheckEndpointBase"/> class.</summary>
    /// <param name="provider">The configuration provider for quality rule lookup.</param>
    /// <param name="dataGateway">The data gateway for live DataSet queries.</param>
    protected ExecuteQualityCheckEndpointBase(QualityConfigurationProvider provider, IDataGateway dataGateway)
    {
        _provider = provider;
        _dataGateway = dataGateway;
    }

    /// <summary>Gets the authorization policy required for read operations.</summary>
    protected virtual string ReadPolicy => "datasets:read";

    /// <summary>Gets the logger instance for this endpoint.</summary>
    protected new ILogger Logger { get; private set; } = null!;

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Post("/quality/rules/{Id}/execute");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s => s.Summary = "Execute a quality check");
    }

    /// <summary>Fetches the quality rule, queries the associated DataSet, and executes the check. Returns Skipped if the rule is disabled, or 404 if the rule is not found.</summary>
    public override async Task HandleAsync(QualityRuleIdRequest req, CancellationToken ct)
    {
        Logger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        var ruleResult = await _provider.GetQualityRule(req.Id, ct).ConfigureAwait(false);

        if (!ruleResult.IsSuccess)
        {
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Failed to fetch quality rule", Details = ruleResult.CurrentMessage }, ct).ConfigureAwait(false);
            return;
        }

        if (ruleResult.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var rule = ruleResult.Value;

        if (!rule.IsEnabled)
        {
            await Send.OkAsync(new QualityCheckResultResponse
            {
                RuleId = req.Id,
                Status = "Skipped",
                FailureCount = 0,
                TotalCount = 0,
                ExecutedAt = DateTime.UtcNow,
                ErrorMessage = "Rule is disabled"
            }, ct).ConfigureAwait(false);
            return;
        }

        var dataCommand = new QueryCommand<Dictionary<string, object>>();

        var dataResult = await _dataGateway.Execute<IEnumerable<Dictionary<string, object>>>(
            dataCommand, new DataStoreTarget(ResolveDataConnectionName(rule.DataSetName), null, rule.DataSetName), ct).ConfigureAwait(false);

        if (!dataResult.IsSuccess)
        {
            await Send.OkAsync(new QualityCheckResultResponse
            {
                RuleId = req.Id,
                Status = "Failed",
                FailureCount = 0,
                TotalCount = 0,
                ExecutedAt = DateTime.UtcNow,
                ErrorMessage = $"Failed to query data: {dataResult.CurrentMessage}"
            }, ct).ConfigureAwait(false);
            return;
        }

        var data = dataResult.Value?.ToList() ?? [];
        var totalCount = data.Count;

        var result = ExecuteRule(rule, data, totalCount);
        result.RuleId = req.Id;
        result.ExecutedAt = DateTime.UtcNow;

        await Send.OkAsync(result, ct).ConfigureAwait(false);
    }

    /// <summary>Resolves the database connection name based on the DataSet name convention.</summary>
    protected virtual string ResolveDataConnectionName(string dataSetName)
    {
        return dataSetName.StartsWith("NFL_", StringComparison.OrdinalIgnoreCase) ||
               dataSetName.Equals("Teams", StringComparison.OrdinalIgnoreCase) ||
               dataSetName.Equals("Players", StringComparison.OrdinalIgnoreCase)
            ? "NflStats"
            : "PlatformConfiguration";
    }

    /// <summary>Executes a single quality rule against the provided data and returns the result.</summary>
    protected virtual QualityCheckResultResponse ExecuteRule(QualityRuleConfiguration rule, IReadOnlyList<Dictionary<string, object>> data, int totalCount)
    {
        return rule.RuleType switch
        {
            "NotNull" => ValidateNotNull(data, rule.FieldName, totalCount),
            "Unique" => ValidateUnique(data, rule.FieldName, totalCount),
            "InRange" => ValidateInRange(data, rule.FieldName, rule.MinValue, rule.MaxValue, totalCount),
            "MatchesPattern" => ValidatePattern(data, rule.FieldName, rule.Pattern, totalCount),
            "CustomExpression" => new QualityCheckResultResponse
            {
                Status = "NotSupported",
                FailureCount = 0,
                TotalCount = totalCount,
                ErrorMessage = "CustomExpression rules are not yet supported"
            },
            _ => new QualityCheckResultResponse
            {
                Status = "Failed",
                FailureCount = 0,
                TotalCount = totalCount,
                ErrorMessage = $"Unknown rule type: {rule.RuleType}"
            }
        };
    }

    /// <summary>Validates that the specified field is not null or empty in all rows.</summary>
    protected static QualityCheckResultResponse ValidateNotNull(IReadOnlyList<Dictionary<string, object>> data, string? fieldName, int totalCount)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return new QualityCheckResultResponse
            {
                Status = "Failed",
                FailureCount = 0,
                TotalCount = totalCount,
                ErrorMessage = "FieldName is required for NotNull rule"
            };
        }

        var failureCount = data.Count(row =>
            !row.ContainsKey(fieldName) ||
            row[fieldName] == null ||
            (row[fieldName] is string str && string.IsNullOrWhiteSpace(str)));

        return new QualityCheckResultResponse
        {
            Status = failureCount == 0 ? "Passed" : "Failed",
            FailureCount = failureCount,
            TotalCount = totalCount
        };
    }

    /// <summary>Validates that the specified field contains unique values across all rows.</summary>
    protected static QualityCheckResultResponse ValidateUnique(IReadOnlyList<Dictionary<string, object>> data, string? fieldName, int totalCount)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return new QualityCheckResultResponse
            {
                Status = "Failed",
                FailureCount = 0,
                TotalCount = totalCount,
                ErrorMessage = "FieldName is required for Unique rule"
            };
        }

        var values = data
            .Where(row => row.ContainsKey(fieldName) && row[fieldName] != null)
            .Select(row => row[fieldName]?.ToString() ?? string.Empty)
            .ToList();

        var uniqueValues = values.Distinct(StringComparer.Ordinal).Count();
        var failureCount = values.Count - uniqueValues;

        return new QualityCheckResultResponse
        {
            Status = failureCount == 0 ? "Passed" : "Failed",
            FailureCount = failureCount,
            TotalCount = totalCount
        };
    }

    /// <summary>Validates that the specified field values fall within the given numeric range.</summary>
    protected static QualityCheckResultResponse ValidateInRange(
        IReadOnlyList<Dictionary<string, object>> data,
        string? fieldName,
        string? minValue,
        string? maxValue,
        int totalCount)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return new QualityCheckResultResponse
            {
                Status = "Failed",
                FailureCount = 0,
                TotalCount = totalCount,
                ErrorMessage = "FieldName is required for InRange rule"
            };
        }

        if (!double.TryParse(minValue, CultureInfo.InvariantCulture, out var min) || !double.TryParse(maxValue, CultureInfo.InvariantCulture, out var max))
        {
            return new QualityCheckResultResponse
            {
                Status = "Failed",
                FailureCount = 0,
                TotalCount = totalCount,
                ErrorMessage = "MinValue and MaxValue must be numeric"
            };
        }

        var failureCount = data.Count(row =>
        {
            if (!row.TryGetValue(fieldName, out var fieldValue) || fieldValue == null)
                return true;

            var valueStr = fieldValue.ToString();
            if (!double.TryParse(valueStr, CultureInfo.InvariantCulture, out var value))
                return true;

            return value < min || value > max;
        });

        return new QualityCheckResultResponse
        {
            Status = failureCount == 0 ? "Passed" : "Failed",
            FailureCount = failureCount,
            TotalCount = totalCount
        };
    }

    /// <summary>Validates that the specified field values match the given regex pattern.</summary>
    protected static QualityCheckResultResponse ValidatePattern(
        IReadOnlyList<Dictionary<string, object>> data,
        string? fieldName,
        string? pattern,
        int totalCount)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return new QualityCheckResultResponse
            {
                Status = "Failed",
                FailureCount = 0,
                TotalCount = totalCount,
                ErrorMessage = "FieldName is required for MatchesPattern rule"
            };
        }

        if (string.IsNullOrWhiteSpace(pattern))
        {
            return new QualityCheckResultResponse
            {
                Status = "Failed",
                FailureCount = 0,
                TotalCount = totalCount,
                ErrorMessage = "Pattern is required for MatchesPattern rule"
            };
        }

        Regex regex;
        try
        {
            regex = new Regex(pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
        }
        catch (Exception ex)
        {
            return new QualityCheckResultResponse
            {
                Status = "Failed",
                FailureCount = 0,
                TotalCount = totalCount,
                ErrorMessage = $"Invalid regex pattern: {ex.Message}"
            };
        }

        var failureCount = data.Count(row =>
        {
            if (!row.TryGetValue(fieldName, out var fieldValue) || fieldValue == null)
                return true;

            var valueStr = fieldValue.ToString();
            if (string.IsNullOrEmpty(valueStr))
                return true;

            try
            {
                return !regex.IsMatch(valueStr);
            }
            catch (System.Text.RegularExpressions.RegexMatchTimeoutException ex)
            {
                _ = ex;
                return true;
            }
        });

        return new QualityCheckResultResponse
        {
            Status = failureCount == 0 ? "Passed" : "Failed",
            FailureCount = failureCount,
            TotalCount = totalCount
        };
    }
}
