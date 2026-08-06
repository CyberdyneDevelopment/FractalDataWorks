namespace Fdw.Services.Quality.Clients;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Quality.Clients.Models;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;

/// <summary>
/// API client for data quality management endpoints.
/// </summary>
public sealed class QualityApiClient : ApiClientBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QualityApiClient"/> class.
    /// </summary>
    public QualityApiClient(HttpClient httpClient, ILogger<QualityApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Gets the quality dashboard data.
    /// </summary>
    /// <returns>A result containing the quality dashboard data.</returns>
    public Task<IGenericResult<QualityDashboardPayload>> GetDashboard(CancellationToken ct = default)
        => Get<QualityDashboardPayload>("quality/dashboard", ct);

    /// <summary>
    /// Gets all quality rules.
    /// </summary>
    /// <returns>A result containing the list of quality rules.</returns>
    public Task<IGenericResult<IReadOnlyList<QualityRuleSummaryPayload>>> GetRules(CancellationToken ct = default)
        => GetList<QualityRuleSummaryPayload>("quality/rules", ct);

    /// <summary>
    /// Gets a specific quality rule by identifier.
    /// </summary>
    /// <returns>A result containing the quality rule detail.</returns>
    public Task<IGenericResult<QualityRuleDetailPayload>> GetRule(Guid id, CancellationToken ct = default)
        => Get<QualityRuleDetailPayload>($"quality/rules/{id}", ct);

    /// <summary>
    /// Creates a new quality rule.
    /// </summary>
    /// <returns>A result containing the created quality rule detail.</returns>
    public Task<IGenericResult<QualityRuleDetailPayload>> CreateRule(CreateQualityRulePayload request, CancellationToken ct = default)
        => Post<CreateQualityRulePayload, QualityRuleDetailPayload>("quality/rules", request, ct);

    /// <summary>
    /// Updates an existing quality rule.
    /// </summary>
    /// <returns>A result containing the updated quality rule detail.</returns>
    public Task<IGenericResult<QualityRuleDetailPayload>> UpdateRule(Guid id, UpdateQualityRulePayload request, CancellationToken ct = default)
        => Put<UpdateQualityRulePayload, QualityRuleDetailPayload>($"quality/rules/{id}", request, ct);

    /// <summary>
    /// Deletes a quality rule.
    /// </summary>
    /// <returns>A result indicating whether the deletion succeeded.</returns>
    public Task<IGenericResult> DeleteRule(Guid id, CancellationToken ct = default)
        => Delete($"quality/rules/{id}", ct);

    /// <summary>
    /// Executes a quality check for a specific rule.
    /// </summary>
    /// <returns>A result containing the quality check result.</returns>
    public Task<IGenericResult<QualityCheckResultPayload>> ExecuteCheck(Guid id, CancellationToken ct = default)
        => PostWithResponse<QualityCheckResultPayload>($"quality/rules/{id}/execute", ct);
}
