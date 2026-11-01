using System.Collections.Generic;

namespace Fdw.Services.Etl.Predicates;

/// <summary>
/// A condition over one record, read once from text and then asked of every record.
/// </summary>
/// <remarks>
/// Why: a Filter stage and a Validate check both state their condition as text in the pipeline's own condition language
/// (see <see cref="RecordPredicates"/>). Reading the text once, before any record is seen, is what lets a condition that
/// cannot be read be refused up front instead of being applied to the data as a guess. This is not the
/// <c>IExpressionEvaluator</c> of the Etl abstractions: no implementation of that interface is wired into a pipeline, so
/// the calculation-engine path is not used here.
/// </remarks>
public interface IRecordPredicate
{
    /// <summary>
    /// Gets whether the record meets the condition.
    /// </summary>
    /// <param name="record">The record, by field name.</param>
    /// <returns><see langword="true"/> when the record meets the condition.</returns>
    bool Evaluate(IDictionary<string, object?> record);
}
