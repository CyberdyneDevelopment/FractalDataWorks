using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Predicates;

/// <summary>
/// Reads the pipeline's condition language: the text a Filter stage's <c>filterExpression</c> and a Validate stage's check
/// <c>expression</c> are written in.
/// </summary>
/// <remarks>
/// <para>
/// A condition is made of comparisons joined by <c>and</c> / <c>&amp;&amp;</c> and <c>or</c> / <c>||</c> (and binds tighter),
/// negated by <c>not</c> or <c>!</c>, and grouped by parentheses. A comparison is a field name followed by one of
/// <c>==</c> <c>!=</c> <c>&gt;=</c> <c>&lt;=</c> <c>&gt;</c> <c>&lt;</c> and a value, or by <c>contains</c> / <c>startswith</c> /
/// <c>endswith</c> and a text, or by nothing (the field holds a true value). <c>== null</c> and <c>!= null</c> ask whether
/// a field holds anything. A value is a quoted text or bare text up to the next connective; keywords are read in any case.
/// </para>
/// <para>
/// Why a strict reader: the text is read once, before any record is seen, and a text that cannot be read is a failure
/// (<c>RecordPredicateInvalid</c>) naming the text and the position, never a condition that every record meets. It
/// replaces the Filter stage's own evaluation, which passed every record when it could not read the text. A field the
/// record lacks makes a comparison false, as it always did; that is a value rule, not a way of passing unreadable text.
/// </para>
/// <para>
/// Why not the framework's filter model (<c>IFilterNode</c>, <c>FilterGroup</c>, <c>FilterCondition</c>, which
/// <c>LoadFilterExpressions</c> reads load-step filters into): it cannot say everything this language accepts. Its
/// <c>LogicalOperator</c> has only And and Or, so it has no way to negate a group, and its filter operators have no negated
/// <c>contains</c>, <c>startswith</c> or <c>endswith</c>, so <c>not</c> cannot be rewritten away either. The Filter stage has
/// always accepted <c>not</c> / <c>!</c> and a bare boolean field, so reading into that model would take a feature away.
/// The framework model exists to be translated into SQL and OData; this one only has to judge a record in memory.
/// </para>
/// <para>
/// This is not the <c>IExpressionEvaluator</c> of the Etl abstractions. No implementation of that interface exists or is
/// wired into a pipeline, and this type does not use it or the calculation-engine path.
/// </para>
/// </remarks>
public static class RecordPredicates
{
    /// <summary>
    /// Reads the text of a condition.
    /// </summary>
    /// <param name="expression">The condition text, after any <c>{{parameter}}</c> placeholders have been replaced, or with them still in place when only its shape is being checked.</param>
    /// <param name="logger">The logger the failure is logged under.</param>
    /// <returns>The condition, or a <c>RecordPredicateInvalid</c> failure naming the text and what could not be read.</returns>
    public static IGenericResult<IRecordPredicate> Parse(string expression, ILogger logger)
    {
        return RecordPredicateScanner.Read(expression, logger);
    }
}
