using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Quality.Services;

/// <summary>
/// Service interface for quality validation rule management and execution.
/// </summary>
public interface IQualityService
{
    /// <summary>
    /// Creates a new quality rule.
    /// </summary>
    /// <param name="rule">The rule configuration to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the created rule configuration.</returns>
    Task<IGenericResult<QualityRuleConfiguration>> CreateRule(QualityRuleConfiguration rule, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing quality rule.
    /// </summary>
    /// <param name="id">The rule identifier.</param>
    /// <param name="rule">The updated rule configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the updated rule configuration.</returns>
    Task<IGenericResult<QualityRuleConfiguration>> UpdateRule(Guid id, QualityRuleConfiguration rule, CancellationToken ct = default);

    /// <summary>
    /// Deletes a quality rule.
    /// </summary>
    /// <param name="id">The rule identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<IGenericResult> DeleteRule(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets a quality rule by identifier.
    /// </summary>
    /// <param name="id">The rule identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the rule configuration.</returns>
    Task<IGenericResult<QualityRuleConfiguration>> GetRule(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets all quality rules for a specific DataSet.
    /// </summary>
    /// <param name="dataSetName">The DataSet name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the list of rule configurations.</returns>
    Task<IGenericResult<IReadOnlyList<QualityRuleConfiguration>>> GetRulesForDataSet(string dataSetName, CancellationToken ct = default);

    /// <summary>
    /// Gets all quality rules.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the list of all rule configurations.</returns>
    Task<IGenericResult<IReadOnlyList<QualityRuleConfiguration>>> GetAllRules(CancellationToken ct = default);

    /// <summary>
    /// Executes a specific quality check.
    /// </summary>
    /// <param name="ruleId">The rule identifier to execute.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the check result.</returns>
    Task<IGenericResult<QualityCheckResult>> ExecuteCheck(Guid ruleId, CancellationToken ct = default);

    /// <summary>
    /// Executes all quality checks for a DataSet.
    /// </summary>
    /// <param name="dataSetName">The DataSet name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the list of check results.</returns>
    Task<IGenericResult<IReadOnlyList<QualityCheckResult>>> ExecuteAllChecks(string dataSetName, CancellationToken ct = default);
}