using System;
using System.Collections.Generic;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections.RowQuery;

namespace Fdw.Services.Connections.Tests.RowQuery;

/// <summary>
/// Unit coverage for <see cref="RecordRowMatcher"/> — the in-memory INNER join resolution (real INNER
/// JOIN semantics: ALL matching parent rows considered, fix #4) and the AND-group equality predicate
/// evaluation (bare column -> child row, dotted "Parent.Col" -> joined parent row), including the
/// case-insensitive value comparison, safe bool-as-string coercion, NULL-never-matches-NULL (fix #2),
/// and the restricted-type-throws-loud (fix #8) semantics.
/// </summary>
public sealed class RecordRowMatcherTests
{
    // Why: a marker IFilterNode implementation this evaluator does not recognise — proves Matches fails
    // loud (fix #3) rather than silently matching everything/nothing for an unhandled node type.
    private sealed class UnrecognisedFilterNode : IFilterNode
    {
    }

    private static IReadOnlyDictionary<string, object?> Row(params (string Key, object? Value)[] fields)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in fields)
            dict[key] = value;
        return dict;
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    public void MatchesJoinedRowReturnsTrueWhenAParentRowMatchesTheJoinKey()
    {
        var parent1 = Row(("RowId", 1L), ("Name", "EnvSecrets"));
        var parent2 = Row(("RowId", 2L), ("Name", "Other"));
        var child = Row(("SecretManagerRowId", 2L));

        var matched = RecordRowMatcher.MatchesJoinedRow(child, [parent1, parent2], "SecretManagerRowId", "RowId", "SecretManager", null);

        matched.ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    public void MatchesJoinedRowReturnsFalseWhenNoParentMatches()
    {
        var parent1 = Row(("RowId", 1L));
        var child = Row(("SecretManagerRowId", 999L));

        var matched = RecordRowMatcher.MatchesJoinedRow(child, [parent1], "SecretManagerRowId", "RowId", "SecretManager", null);

        matched.ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    public void MatchesJoinedRowReturnsFalseWhenChildJoinKeyIsMissing()
    {
        var parent1 = Row(("RowId", 1L));
        var child = Row(("SomeOtherColumn", 1L));

        var matched = RecordRowMatcher.MatchesJoinedRow(child, [parent1], "SecretManagerRowId", "RowId", "SecretManager", null);

        matched.ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    public void MatchesJoinedRowToleratesGuidVsStringTypeMismatch()
    {
        var guid = Guid.NewGuid();
        var parent1 = Row(("Id", guid.ToString()));
        var child = Row(("SecretManagerId", guid));

        var matched = RecordRowMatcher.MatchesJoinedRow(child, [parent1], "SecretManagerId", "Id", "SecretManager", null);

        matched.ShouldBeTrue();
    }

    // ── Fix #4: real INNER JOIN semantics — ALL matching parent rows considered ────

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "InnerJoinSemantics")]
    public void MatchesJoinedRowKeepsTheChildWhenAnyOfSeveralParentRowsSharingTheJoinKeySatisfiesTheFilter()
    {
        // Why (fix #4): files carry no PK enforcement, so two parent rows can share a join-key value.
        // SQL's INNER JOIN produces a row for EVERY matching pairing and keeps the child if ANY pairing
        // satisfies the WHERE clause — first-match-only resolution would silently drop this child row
        // because the FIRST parent with RowId=1 does not satisfy the filter, only the second one does.
        var parent1 = Row(("RowId", 1L), ("Name", "Other"));
        var parent2 = Row(("RowId", 1L), ("Name", "EnvSecrets"));
        var child = Row(("SecretManagerRowId", 1L));
        var filter = new FilterCondition { PropertyName = "SecretManager.Name", Operator = new EqualOperator(), Value = "EnvSecrets" };

        var matched = RecordRowMatcher.MatchesJoinedRow(child, [parent1, parent2], "SecretManagerRowId", "RowId", "SecretManager", filter);

        matched.ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "InnerJoinSemantics")]
    public void MatchesJoinedRowExcludesTheChildWhenNoPairingSharingTheJoinKeySatisfiesTheFilter()
    {
        var parent1 = Row(("RowId", 1L), ("Name", "Other"));
        var parent2 = Row(("RowId", 1L), ("Name", "AlsoOther"));
        var child = Row(("SecretManagerRowId", 1L));
        var filter = new FilterCondition { PropertyName = "SecretManager.Name", Operator = new EqualOperator(), Value = "EnvSecrets" };

        var matched = RecordRowMatcher.MatchesJoinedRow(child, [parent1, parent2], "SecretManagerRowId", "RowId", "SecretManager", filter);

        matched.ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    public void MatchesReturnsTrueForANullFilter()
    {
        var row = Row(("Name", "EnvSecrets"));

        RecordRowMatcher.Matches(row, null, null, null).ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    public void MatchesEvaluatesABareColumnAgainstTheChildRow()
    {
        var row = Row(("Name", "EnvSecrets"));
        var condition = new FilterCondition { PropertyName = "Name", Operator = new EqualOperator(), Value = "EnvSecrets" };

        RecordRowMatcher.Matches(row, null, null, condition).ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    public void MatchesEvaluatesADottedColumnAgainstTheJoinedParentRow()
    {
        var child = Row(("SecretManagerRowId", 1L));
        var parent = Row(("Id", "11111111-1111-1111-1111-111111111111"));
        var condition = new FilterCondition
        {
            PropertyName = "SecretManager.Id",
            Operator = new EqualOperator(),
            Value = Guid.Parse("11111111-1111-1111-1111-111111111111")
        };

        RecordRowMatcher.Matches(child, parent, "SecretManager", condition).ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    public void MatchesReturnsFalseWhenTheDottedQualifierDoesNotMatchTheParentContainerName()
    {
        var child = Row(("SecretManagerRowId", 1L));
        var parent = Row(("Id", "11111111-1111-1111-1111-111111111111"));
        var condition = new FilterCondition
        {
            PropertyName = "UnknownContainer.Id",
            Operator = new EqualOperator(),
            Value = Guid.Parse("11111111-1111-1111-1111-111111111111")
        };

        RecordRowMatcher.Matches(child, parent, "SecretManager", condition).ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    public void MatchesEvaluatesAnAndGroupRequiringEveryConditionToMatch()
    {
        var row = Row(("IsCurrent", true), ("IsDeleted", false));
        var group = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes =
            [
                new FilterCondition { PropertyName = "IsCurrent", Operator = new EqualOperator(), Value = true },
                new FilterCondition { PropertyName = "IsDeleted", Operator = new EqualOperator(), Value = false }
            ]
        };

        RecordRowMatcher.Matches(row, null, null, group).ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    public void MatchesEvaluatesAnAndGroupWhereOneConditionFails()
    {
        var row = Row(("IsCurrent", true), ("IsDeleted", true));
        var group = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes =
            [
                new FilterCondition { PropertyName = "IsCurrent", Operator = new EqualOperator(), Value = true },
                new FilterCondition { PropertyName = "IsDeleted", Operator = new EqualOperator(), Value = false }
            ]
        };

        RecordRowMatcher.Matches(row, null, null, group).ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    public void MatchesTreatsNumericValuesAsEqualToNativeIntValues()
    {
        var row = Row(("RowId", 1L));
        var condition = new FilterCondition { PropertyName = "RowId", Operator = new EqualOperator(), Value = 1 };

        RecordRowMatcher.Matches(row, null, null, condition).ShouldBeTrue();
    }

    // ── Fix #2: SQL equality never matches NULL to NULL ────────────────────────────

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "NullEquality")]
    public void MatchesReturnsFalseWhenBothTheRowValueAndTheFilterValueAreNull()
    {
        // Why (fix #2): "WHERE x = NULL" never matches in SQL — a filter condition comparing a
        // genuinely-absent row value against a null filter Value must NOT match, unlike the previous
        // left==null && right==null => true behavior.
        var row = Row(("Description", null));
        var condition = new FilterCondition { PropertyName = "Description", Operator = new EqualOperator(), Value = null };

        RecordRowMatcher.Matches(row, null, null, condition).ShouldBeFalse();
    }

    // ── Fix #3: an unrecognised IFilterNode type fails loud, never silently matches ─

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "UnsupportedGrammar")]
    public void MatchesThrowsForAnUnrecognisedFilterNodeType()
    {
        var row = Row(("Name", "EnvSecrets"));

        Should.Throw<InvalidOperationException>(() => RecordRowMatcher.Matches(row, null, null, new UnrecognisedFilterNode()));
    }

    // ── Fix #8: restrict comparison to supported CLR shapes — fail loud, never fabricate ─

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "UnsupportedValueType")]
    public void MatchesThrowsForAnUnsupportedComparisonValueType()
    {
        // Why (fix #8): a catch-all Convert.ToString comparison would have compared two distinct
        // byte[] instances as EQUAL (both stringify to "System.Byte[]") — fail loud instead.
        var row = Row(("Payload", new byte[] { 1, 2, 3 }));
        var condition = new FilterCondition { PropertyName = "Payload", Operator = new EqualOperator(), Value = new byte[] { 1, 2, 3 } };

        Should.Throw<InvalidOperationException>(() => RecordRowMatcher.Matches(row, null, null, condition));
    }

    // ── Fix (a): string VALUE comparisons are case-insensitive ────────────────────

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "CaseInsensitivity")]
    public void MatchesTreatsStringValuesAsCaseInsensitive()
    {
        // Why: SQL runs CI collation, so a config lookup like Get("envsecrets") must match a stored
        // "EnvSecrets" — the FileSystem/Json in-memory path must behave the same as MsSql.
        var row = Row(("Name", "EnvSecrets"));
        var condition = new FilterCondition { PropertyName = "Name", Operator = new EqualOperator(), Value = "envsecrets" };

        RecordRowMatcher.Matches(row, null, null, condition).ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "CaseInsensitivity")]
    public void MatchesTreatsDifferingStringValuesAsUnequalRegardlessOfCase()
    {
        var row = Row(("Name", "EnvSecrets"));
        var condition = new FilterCondition { PropertyName = "Name", Operator = new EqualOperator(), Value = "othersecrets" };

        RecordRowMatcher.Matches(row, null, null, condition).ShouldBeFalse();
    }

    // ── Fix (b): bool-as-string coercion never throws ──────────────────────────────

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "BoolCoercion")]
    public void MatchesCoercesTheStringZeroAgainstANativeFalseWithoutThrowing()
    {
        // Why: Convert.ToBoolean("0") throws FormatException — a decoded bit column may arrive as the
        // literal string "0" while the config filter always carries IsDeleted as a native bool.
        var row = Row(("IsDeleted", "0"));
        var condition = new FilterCondition { PropertyName = "IsDeleted", Operator = new EqualOperator(), Value = false };

        Should.NotThrow(() => RecordRowMatcher.Matches(row, null, null, condition).ShouldBeTrue());
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "BoolCoercion")]
    public void MatchesCoercesTheStringOneAgainstANativeTrueWithoutThrowing()
    {
        var row = Row(("IsCurrent", "1"));
        var condition = new FilterCondition { PropertyName = "IsCurrent", Operator = new EqualOperator(), Value = true };

        Should.NotThrow(() => RecordRowMatcher.Matches(row, null, null, condition).ShouldBeTrue());
    }
}
