using System;
using System.Collections.Generic;
using System.Globalization;
using Fdw.Data;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Connections.RowQuery;

/// <summary>
/// Evaluates the (grammar-validated — see <see cref="RecordQueryValidator"/>) AND-of-equality
/// predicate tree of a <c>QueryCommand</c> against in-memory row dictionaries, and resolves the single
/// supported INNER join (child row → same-path parent rows) with real INNER JOIN semantics: ALL
/// matching parent rows are considered, and the child row survives if ANY pairing satisfies the
/// parent-qualified filter.
/// </summary>
/// <remarks>
/// Format-agnostic, transport-agnostic: operates purely on <see cref="IReadOnlyDictionary{TKey,TValue}"/>
/// rows (the shape every <c>IRowSourceReader</c>/<c>DataRecord.ToDictionary()</c> decode produces) — it
/// never touches a file, a socket, or a specific serialization format. Any record-connector-based
/// connection (FileSystem today, Http later) can reuse it once it has decoded rows in this shape.
/// </remarks>
public static class RecordRowMatcher
{
    /// <summary>
    /// Determines whether <paramref name="childRow"/> survives an INNER JOIN against
    /// <paramref name="parentRows"/> on <paramref name="leftField"/> = <paramref name="rightField"/>
    /// AND satisfies <paramref name="filter"/> for at least one matching parent pairing.
    /// </summary>
    /// <remarks>
    /// Why this is real INNER JOIN semantics, not first-match-only: files carry no PK enforcement, so
    /// two parent rows can legitimately share a join-key value. SQL's INNER JOIN produces a row for
    /// EVERY matching pairing and keeps the child if any pairing satisfies the WHERE clause; resolving
    /// only the FIRST matching parent silently drops a child row a real INNER JOIN would have kept
    /// (NO FALLBACKS — this is a correctness gap, not a convenience shortcut).
    /// </remarks>
    public static bool MatchesJoinedRow(
        IReadOnlyDictionary<string, object?> childRow,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> parentRows,
        string leftField,
        string rightField,
        string parentContainerName,
        IFilterNode? filter)
    {
        if (!childRow.TryGetValue(leftField, out var leftValue) || leftValue is null)
            return false;

        for (var i = 0; i < parentRows.Count; i++)
        {
            var parentRow = parentRows[i];
            if (!parentRow.TryGetValue(rightField, out var rightValue) || !ValuesEqual(leftValue, rightValue))
                continue;

            if (Matches(childRow, parentRow, parentContainerName, filter))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Evaluates <paramref name="node"/> (a <see cref="FilterCondition"/> leaf or a
    /// <see cref="FilterGroup"/> composite) against <paramref name="childRow"/> and the optional
    /// joined <paramref name="parentRow"/>. A bare column name resolves against the child row; a
    /// dotted <c>"{parentContainerName}.Col"</c> qualifier resolves against the parent row.
    /// </summary>
    public static bool Matches(
        IReadOnlyDictionary<string, object?> childRow,
        IReadOnlyDictionary<string, object?>? parentRow,
        string? parentContainerName,
        IFilterNode? node)
    {
        switch (node)
        {
            case null:
                return true;

            case FilterCondition condition:
                return EvaluateCondition(childRow, parentRow, parentContainerName, condition);

            case FilterGroup group:
                return EvaluateGroup(childRow, parentRow, parentContainerName, group);

            // Why: RecordQueryValidator.ValidateShape already rejects any unrecognised IFilterNode
            // BEFORE evaluation reaches here (fix #3) — an unhandled type at this point means the
            // validator's grammar coverage has a gap, not that this is a data condition to tolerate.
            // Throwing (not silently matching everything, and not silently matching nothing) surfaces
            // that gap immediately instead of masquerading as an empty/full result set.
            default:
                throw new InvalidOperationException(
                    $"Unsupported filter node type '{node.GetType().Name}' reached the matcher — RecordQueryValidator should have rejected it first.");
        }
    }

    private static bool EvaluateGroup(
        IReadOnlyDictionary<string, object?> childRow,
        IReadOnlyDictionary<string, object?>? parentRow,
        string? parentContainerName,
        FilterGroup group)
    {
        if (ReferenceEquals(group.Operator, LogicalOperator.Or))
        {
            foreach (var child in group.Nodes)
            {
                if (Matches(childRow, parentRow, parentContainerName, child))
                    return true;
            }
            return false;
        }

        foreach (var child in group.Nodes)
        {
            if (!Matches(childRow, parentRow, parentContainerName, child))
                return false;
        }
        return true;
    }

    private static bool EvaluateCondition(
        IReadOnlyDictionary<string, object?> childRow,
        IReadOnlyDictionary<string, object?>? parentRow,
        string? parentContainerName,
        FilterCondition condition)
    {
        var (row, columnName) = ResolveTarget(childRow, parentRow, parentContainerName, condition.PropertyName);
        if (row is null)
            return false;

        row.TryGetValue(columnName, out var actual);
        return ValuesEqual(actual, condition.Value);
    }

    // Why: strip the "Container." qualifier to the bare column, matching the SQL translators'
    // BuildWhereClause qualification convention (bare -> primary/child table, dotted -> named table).
    // The qualifier is validated against the container tree upstream (RecordQueryEvaluator, fix #2) —
    // an unrecognised qualifier reaching this point is already a fail-loud condition raised earlier, so
    // this resolves rather than re-validating.
    private static (IReadOnlyDictionary<string, object?>? Row, string Column) ResolveTarget(
        IReadOnlyDictionary<string, object?> childRow,
        IReadOnlyDictionary<string, object?>? parentRow,
        string? parentContainerName,
        string propertyName)
    {
        var (qualifier, column) = QualifiedColumn.Split(propertyName);
        if (qualifier is null)
            return (childRow, column);

        return parentContainerName is not null && string.Equals(qualifier, parentContainerName, StringComparison.Ordinal)
            ? (parentRow, column)
            : (null, column);
    }

    // Why: type-tolerant equality — decoded rows carry raw format primitives (JSON numbers arrive as
    // long/double, Guid/bool columns arrive as strings), and the filter's Value may be a native
    // Guid/bool/int from the query builder. Compare by coercing to a common representation rather than
    // requiring identical CLR types.
    // Why (fix #2 NULL semantics): SQL equality never matches NULL to NULL — "WHERE x = NULL" never
    // matches in SQL (IS NULL is the only way to test nullity, which this AND-of-equality grammar does
    // not carry). left==null && right==null now returns FALSE, not TRUE, matching that semantics.
    internal static bool ValuesEqual(object? left, object? right)
    {
        if (left is null || right is null)
            return false;

        // Why: each CLR-shape comparison is its own small helper (not inlined branches here) so this
        // dispatcher stays a flat sequence of type-shape checks — keeps cyclomatic complexity low while
        // still restricting comparison to the shapes this grammar was designed for (fix #8).
        if (TryCompareGuid(left, right, out var guidEqual))
            return guidEqual;

        if (TryCompareBoolean(left, right, out var boolEqual))
            return boolEqual;

        if (TryCompareNumeric(left, right, out var numericEqual))
            return numericEqual;

        if (TryCompareString(left, right, out var stringEqual))
            return stringEqual;

        if (TryCompareDateTime(left, right, out var dateEqual))
            return dateEqual;

        // Why (fix #8): restrict comparison to the CLR shapes that can actually arrive from a decoded
        // record row or a filter's Value (string/bool/Guid/numeric/DateTime family — all handled above).
        // The removed catch-all Convert.ToString comparison silently compared unrelated types by their
        // ToString() (e.g. two distinct byte[] instances both stringify to "System.Byte[]" and would
        // compare EQUAL) — fail loud instead of fabricating equality for a type this grammar was never
        // designed to compare (NO FALLBACKS).
        throw new InvalidOperationException(
            $"Unsupported comparison value type '{left.GetType().Name}'/'{right.GetType().Name}' — only string, bool, Guid, numeric, DateTime and DateTimeOffset values can be compared.");
    }

    private static bool TryCompareGuid(object left, object right, out bool equal)
    {
        if (left is not Guid && right is not Guid)
        {
            equal = false;
            return false;
        }

        var leftGuid = ToGuid(left);
        var rightGuid = ToGuid(right);
        equal = leftGuid.HasValue && rightGuid.HasValue && leftGuid.Value == rightGuid.Value;
        return true;
    }

    private static bool TryCompareBoolean(object left, object right, out bool equal)
    {
        if (left is not bool && right is not bool)
        {
            equal = false;
            return false;
        }

        equal = ToBoolean(left) == ToBoolean(right);
        return true;
    }

    private static bool TryCompareNumeric(object left, object right, out bool equal)
    {
        if (!IsNumeric(left) || !IsNumeric(right))
        {
            equal = false;
            return false;
        }

        equal = Convert.ToDecimal(left, CultureInfo.InvariantCulture) == Convert.ToDecimal(right, CultureInfo.InvariantCulture);
        return true;
    }

    // Why (fix a, kept): SQL runs case-insensitive collation, so a config lookup like
    // Get("envsecrets") must match a stored "EnvSecrets" — string VALUE comparisons are
    // case-insensitive. Column-NAME lookups are already case-insensitive because every row
    // dictionary this matcher sees is built with StringComparer.OrdinalIgnoreCase
    // (DataRecord.ToDictionary()).
    private static bool TryCompareString(object left, object right, out bool equal)
    {
        if (left is not string && right is not string)
        {
            equal = false;
            return false;
        }

        equal = string.Equals(
            Convert.ToString(left, CultureInfo.InvariantCulture),
            Convert.ToString(right, CultureInfo.InvariantCulture),
            StringComparison.OrdinalIgnoreCase);
        return true;
    }

    private static bool TryCompareDateTime(object left, object right, out bool equal)
    {
        var leftIsDate = left is DateTime or DateTimeOffset;
        var rightIsDate = right is DateTime or DateTimeOffset;
        if (!leftIsDate || !rightIsDate)
        {
            equal = false;
            return false;
        }

        equal = ToDateTimeOffset(left) == ToDateTimeOffset(right);
        return true;
    }

    private static Guid? ToGuid(object value) => value switch
    {
        Guid guid => guid,
        string text when Guid.TryParse(text, out var parsed) => parsed,
        _ => null,
    };

    // Why (fix b, kept): Convert.ToBoolean(string) only accepts "True"/"False" (any case) and THROWS
    // FormatException for "0"/"1" — but the config filter always carries IsCurrent/IsDeleted as a
    // native bool, while a JSON/text-decoded row may carry the same bit column as the literal string
    // "0" or "1". Parse those explicitly before falling back to Convert.ToBoolean so a bit-as-string
    // column never throws — it must coerce, never throw (NO FALLBACKS: an unrecognized string still
    // throws via Convert.ToBoolean, which is the correct fail-loud behavior for a genuinely bad value).
    private static bool ToBoolean(object value) => value switch
    {
        bool flag => flag,
        string text when bool.TryParse(text, out var parsedBool) => parsedBool,
        string text when int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt) => parsedInt != 0,
        long integral => integral != 0,
        int integer => integer != 0,
        decimal number => number != 0,
        double real => real != 0,
        _ => Convert.ToBoolean(value, CultureInfo.InvariantCulture),
    };

    private static DateTimeOffset ToDateTimeOffset(object value) => value switch
    {
        DateTimeOffset dateTimeOffset => dateTimeOffset,
        DateTime dateTime => new DateTimeOffset(dateTime),
        _ => throw new InvalidOperationException($"Unsupported date/time value type '{value.GetType().Name}'."),
    };

    private static bool IsNumeric(object value) =>
        value is long or int or short or decimal or double or float;
}
