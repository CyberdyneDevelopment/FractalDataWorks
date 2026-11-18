using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Data.Abstractions.DataConfigurationRules;

/// <summary>
/// The data configuration rules and their values (FDW-6345): a rule records which values were picked from a container and column, and
/// the path a consumer follows to use them. Every write keeps the old version, is recorded in the administrative audit, and runs in
/// one transaction on the options store (write, record, commit); a removed rule is kept and can be restored.
/// </summary>
public interface IDataConfigurationRules
{
    /// <summary>Lists a page of the recorded changes of a container's rules, newest first (FDW-6571).</summary>
    /// <param name="query">What to list.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The page; or the failure, including a recorded change whose kind the catalog does not know.</returns>
    Task<IGenericResult<DataConfigurationRuleHistoryPage>> History(DataConfigurationRuleHistoryQuery query, CancellationToken cancellationToken = default);

    /// <summary>Reads the longest text each field of a rule and of a value takes, from the fields the two rule tables declare (FDW-6575).</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The declared lengths; or the failure, such as a container that cannot be reached or a declared length that is not a whole number.</returns>
    Task<IGenericResult<DataConfigurationRuleLimits>> Limits(CancellationToken cancellationToken = default);

    /// <summary>Lists a page of a container's rules.</summary>
    /// <param name="query">What to list.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The page, or the failure.</returns>
    Task<IGenericResult<DataConfigurationRulesPage>> List(DataConfigurationRulesQuery query, CancellationToken cancellationToken = default);

    /// <summary>Gets one rule with its values.</summary>
    /// <param name="key">The rule.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The rule, or the failure that says it is not found.</returns>
    Task<IGenericResult<DataConfigurationRule>> Get(DataConfigurationRuleKey key, CancellationToken cancellationToken = default);

    /// <summary>Adds a rule with its values.</summary>
    /// <param name="rule">The rule to add.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The rule as stored, or the failure.</returns>
    Task<IGenericResult<DataConfigurationRule>> Add(NewDataConfigurationRule rule, CancellationToken cancellationToken = default);

    /// <summary>Changes a rule; the stated fields change and the rest keep their stored value.</summary>
    /// <param name="change">The change.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The rule as stored, or the failure.</returns>
    Task<IGenericResult<DataConfigurationRule>> Change(DataConfigurationRuleChange change, CancellationToken cancellationToken = default);

    /// <summary>Switches a rule on or off; a rule that is off is kept and does nothing.</summary>
    /// <param name="key">The rule.</param>
    /// <param name="active">Whether the rule is switched on.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The rule as stored, or the failure.</returns>
    Task<IGenericResult<DataConfigurationRule>> Switch(DataConfigurationRuleKey key, bool active, CancellationToken cancellationToken = default);

    /// <summary>Removes a rule; it is kept, marked deleted, and can be restored.</summary>
    /// <param name="key">The rule.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The rule as stored, or the failure.</returns>
    Task<IGenericResult<DataConfigurationRule>> Remove(DataConfigurationRuleKey key, CancellationToken cancellationToken = default);

    /// <summary>Restores a removed rule, switched off.</summary>
    /// <param name="key">The rule.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The rule as stored, or the failure.</returns>
    Task<IGenericResult<DataConfigurationRule>> Restore(DataConfigurationRuleKey key, CancellationToken cancellationToken = default);

    /// <summary>Adds values to a rule.</summary>
    /// <param name="key">The rule.</param>
    /// <param name="values">The values to add.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The rule as stored, or the failure.</returns>
    Task<IGenericResult<DataConfigurationRule>> AddValues(DataConfigurationRuleKey key, IReadOnlyList<NewDataConfigurationRuleValue> values, CancellationToken cancellationToken = default);

    /// <summary>Changes one value of a rule.</summary>
    /// <param name="change">The change.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The rule as stored, or the failure.</returns>
    Task<IGenericResult<DataConfigurationRule>> ChangeValue(DataConfigurationRuleValueChange change, CancellationToken cancellationToken = default);

    /// <summary>Removes values of a rule, all or none.</summary>
    /// <param name="key">The rule.</param>
    /// <param name="valueIds">The values to remove.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The rule as stored, or the failure.</returns>
    Task<IGenericResult<DataConfigurationRule>> RemoveValues(DataConfigurationRuleKey key, IReadOnlyList<Guid> valueIds, CancellationToken cancellationToken = default);
}
