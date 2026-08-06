using System.Collections.Generic;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections.RowQuery;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Tests.RowQuery;

/// <summary>
/// Unit coverage for <see cref="RecordQueryValidator"/> — the AND-of-equality, at-most-one-INNER-join
/// query-shape guard <see cref="RecordQueryEvaluator"/> runs before <see cref="RecordRowMatcher"/>
/// evaluates a command. Anything outside that grammar must fail loud, never silently match/miss rows.
/// </summary>
public sealed class RecordQueryValidatorTests
{
    // Why: a marker IFilterNode implementation this validator's grammar does not recognise — proves
    // ValidateShape fails loud (fix #3) instead of the old `default: return GenericResult.Success()`
    // which validated an unrecognised node as "supported" and let it match NOTHING downstream.
    private sealed class UnrecognisedFilterNode : IFilterNode
    {
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "UnsupportedGrammar")]
    public void ValidateShapeRejectsAnUnrecognisedFilterNodeType()
    {
        var result = RecordQueryValidator.ValidateShape(new UnrecognisedFilterNode(), [], NullLogger.Instance);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "UnsupportedGrammar")]
    public void ValidateShapeRejectsAnUnrecognisedFilterNodeTypeNestedInAnAndGroup()
    {
        var group = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes =
            [
                new FilterCondition { PropertyName = "IsCurrent", Operator = new EqualOperator(), Value = true },
                new UnrecognisedFilterNode()
            ]
        };

        var result = RecordQueryValidator.ValidateShape(group, [], NullLogger.Instance);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    public void ValidateShapeSucceedsForANullFilterAndNoJoins()
    {
        var result = RecordQueryValidator.ValidateShape(null, [], NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    public void ValidateShapeSucceedsForASingleEqualityCondition()
    {
        var condition = new FilterCondition { PropertyName = "Name", Operator = new EqualOperator(), Value = "EnvSecrets" };

        var result = RecordQueryValidator.ValidateShape(condition, [], NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    public void ValidateShapeSucceedsForAnAndGroupOfEqualityConditions()
    {
        var group = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes =
            [
                new FilterCondition { PropertyName = "IsCurrent", Operator = new EqualOperator(), Value = true },
                new FilterCondition { PropertyName = "IsDeleted", Operator = new EqualOperator(), Value = false }
            ]
        };

        var result = RecordQueryValidator.ValidateShape(group, [], NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "OperatorRejection")]
    public void ValidateShapeRejectsANonEqualityOperator()
    {
        var condition = new FilterCondition { PropertyName = "Name", Operator = new NotEqualOperator(), Value = "EnvSecrets" };

        var result = RecordQueryValidator.ValidateShape(condition, [], NullLogger.Instance);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "OperatorRejection")]
    public void ValidateShapeRejectsATopLevelOrGroup()
    {
        var group = new FilterGroup
        {
            Operator = LogicalOperator.Or,
            Nodes =
            [
                new FilterCondition { PropertyName = "Name", Operator = new EqualOperator(), Value = "A" },
                new FilterCondition { PropertyName = "Name", Operator = new EqualOperator(), Value = "B" }
            ]
        };

        var result = RecordQueryValidator.ValidateShape(group, [], NullLogger.Instance);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "SingleJoin")]
    public void ValidateShapeAcceptsASingleInnerJoinWithOneConditionPair()
    {
        var joins = new List<IJoinExpression>
        {
            new JoinExpression
            {
                TargetContainerName = "SecretManager",
                JoinType = "INNER",
                JoinConditions = [("SecretManagerRowId", "RowId")]
            }
        };

        var result = RecordQueryValidator.ValidateShape(null, joins, NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "SingleJoin")]
    public void ValidateShapeRejectsMultipleJoins()
    {
        var joins = new List<IJoinExpression>
        {
            new JoinExpression { TargetContainerName = "SecretManager", JoinType = "INNER", JoinConditions = [("SecretManagerRowId", "RowId")] },
            new JoinExpression { TargetContainerName = "Other", JoinType = "INNER", JoinConditions = [("OtherRowId", "RowId")] }
        };

        var result = RecordQueryValidator.ValidateShape(null, joins, NullLogger.Instance);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "SingleJoin")]
    public void ValidateShapeRejectsALeftJoin()
    {
        var joins = new List<IJoinExpression>
        {
            new JoinExpression { TargetContainerName = "SecretManager", JoinType = "LEFT", JoinConditions = [("SecretManagerRowId", "RowId")] }
        };

        var result = RecordQueryValidator.ValidateShape(null, joins, NullLogger.Instance);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "SingleJoin")]
    public void ValidateShapeRejectsAJoinWithCompositeConditionPairs()
    {
        var joins = new List<IJoinExpression>
        {
            new JoinExpression
            {
                TargetContainerName = "SecretManager",
                JoinType = "INNER",
                JoinConditions = [("SecretManagerRowId", "RowId"), ("TenantRowId", "TenantRowId")]
            }
        };

        var result = RecordQueryValidator.ValidateShape(null, joins, NullLogger.Instance);

        result.IsSuccess.ShouldBeFalse();
    }
}
