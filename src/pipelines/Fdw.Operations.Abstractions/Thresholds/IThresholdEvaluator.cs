using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>
/// The one evaluator of thresholds (FDW-3936). A subject reports its values at its own point (a component finished, a run ended, a connect attempt) and calls
/// <see cref="Report"/>; it judges nothing itself.
/// </summary>
public interface IThresholdEvaluator
{
    /// <summary>Judges every enabled threshold attached to the subject against the values and fires the consequence of each rule that matches.</summary>
    /// <param name="subject">What the values are about.</param>
    /// <param name="values">The values, by field name. A field a threshold rule names that is not here leaves that rule not measured.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>
    /// Which rules fired and what their consequences came to. A subject with no threshold attached is not judged: the report is empty, with 0 thresholds judged. A failure when
    /// the thresholds cannot be read; a consequence that fails is in the report, and does not stop the rules after it.
    /// </returns>
    Task<IGenericResult<ThresholdReport>> Report(ThresholdSubject subject, IReadOnlyDictionary<string, object?> values, CancellationToken cancellationToken = default);

    /// <summary>
    /// Judges a set of thresholds the caller already holds, instead of the ones attached to the subject. A caller that resolves which thresholds apply by its own rule
    /// (a layered one, for example) uses this; the judging, the entry tracking and the consequences are the same as <see cref="Report"/>.
    /// </summary>
    /// <param name="subject">What the values are about.</param>
    /// <param name="thresholds">The thresholds to judge.</param>
    /// <param name="values">The values, by field name.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The same report as <see cref="Report"/>.</returns>
    Task<IGenericResult<ThresholdReport>> Judge(
        ThresholdSubject subject, IReadOnlyList<IThresholdDefinition> thresholds, IReadOnlyDictionary<string, object?> values, CancellationToken cancellationToken = default);

    /// <summary>Finds the rules of the thresholds that name a consequence and whose criteria hold, without firing anything and without touching the entry tracking.</summary>
    /// <param name="subject">What the values are about.</param>
    /// <param name="thresholds">The thresholds to read.</param>
    /// <param name="values">The values, by field name. A rule that names a value not reported does not match.</param>
    /// <param name="escalation">The consequence to look for, such as HoldNewRuns.</param>
    /// <returns>The matching rules, in the order they are stated.</returns>
    IReadOnlyList<ThresholdMatch> Holds(
        ThresholdSubject subject, IReadOnlyList<IThresholdDefinition> thresholds, IReadOnlyDictionary<string, object?> values, string escalation);
}
