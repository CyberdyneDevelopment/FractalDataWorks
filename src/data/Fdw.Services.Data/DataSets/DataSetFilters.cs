using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.DataSets.Results;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>
/// A data set's own filter (FDW-4997): its stored conditions checked and built into the filter every read of it carries.
/// </summary>
/// <remarks>
/// <para>
/// Each condition names a stored, non-calculated field of the data set (the store has a column for it), a <see cref="FilterOperators"/> name
/// (no alias stands in for one) and a <see cref="DataTypes"/> name its value is read as. An operator that compares with a value needs one and
/// one that takes none (IsNull, IsNotNull) states none; the value is converted to its data type here, once, so the store is sent a typed
/// value. Each broken rule is refused with its own code.
/// </para>
/// <para>
/// The same rule runs when the data set is saved, so a condition that cannot be kept is refused before anything is written, and again
/// when it is read, so a condition stored some other way is refused rather than dropped.
/// </para>
/// </remarks>
public static class DataSetFilters
{
    /// <summary>Checks a data set's own conditions and builds them into one filter: each condition, ANDed in ordinal order.</summary>
    /// <param name="dataSet">The data set, as it will be saved or as it was read.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The filter, or null when the data set states no condition; or the refusal naming the first condition that breaks a rule.</returns>
    public static IGenericResult<IFilterExpression?> Expression(IDataSetImplementationConfiguration dataSet, ILogger logger)
    {
        var conditions = dataSet.Filters.Where(f => f.IsCurrent && !f.IsDeleted).OrderBy(f => f.Ordinal).ToList();
        if (conditions.Count == 0)
            return GenericResult<IFilterExpression?>.Success(null);

        var stored = dataSet.Fields.Where(f => f.IsCurrent && !f.IsDeleted && !f.IsCalculated).Select(f => f.Name).ToHashSet(StringComparer.Ordinal);
        var nodes = new List<IFilterNode>(conditions.Count);
        foreach (var filter in conditions)
        {
            var condition = Condition(dataSet.Name, filter, stored, logger);
            if (condition.IsFailure || condition.Value is not { } node)
                return condition.ToNewResult<IFilterExpression?>();

            nodes.Add(node);
        }

        IFilterNode root = nodes.Count == 1 ? nodes[0] : new FilterGroup { Operator = LogicalOperator.And, Nodes = nodes };
        return GenericResult<IFilterExpression?>.Success(new FilterExpression { Root = root });
    }

    // One condition, checked rule by rule in the order a person fixes them: the field, the operator, the data type, then the value.
    private static IGenericResult<IFilterNode> Condition(
        string dataSetName, DataSetFilterConditionConfiguration filter, HashSet<string> stored, ILogger logger)
    {
        if (!stored.Contains(filter.FieldName))
            return Refused("DataSetFilterFieldUnknown", ResultDetails.Create("name", dataSetName, "ordinal", filter.Ordinal, "field", filter.FieldName), logger);

        var op = FilterOperators.ByName(filter.Operator);
        if (ReferenceEquals(op, FilterOperators.NotFound))
            return Refused("DataSetFilterOperatorUnknown", ResultDetails.Create("name", dataSetName, "ordinal", filter.Ordinal, "operator", filter.Operator)
                .With("operators", string.Join(", ", FilterOperators.All().Select(o => o.Name))), logger);

        if (DataTypeNames.Find(filter.DataType) is not { } dataType)
            return Refused("DataSetFilterDataTypeUnknown", ResultDetails.Create("name", dataSetName, "ordinal", filter.Ordinal, "dataType", filter.DataType)
                .With("dataTypes", string.Join(", ", DataTypes.All().Select(t => t.Name))), logger);

        if (!op.RequiresValue)
            return filter.Value is null
                ? GenericResult<IFilterNode>.Success(new FilterCondition { PropertyName = filter.FieldName, Operator = op })
                : Refused("DataSetFilterValueNotAllowed", Described(dataSetName, filter).With("value", filter.Value), logger);

        if (filter.Value is null)
            return Refused("DataSetFilterValueRequired", Described(dataSetName, filter), logger);

        var value = dataType.Convert(filter.Value);
        return value.IsSuccess
            ? GenericResult<IFilterNode>.Success(new FilterCondition { PropertyName = filter.FieldName, Operator = op, Value = value.Value })
            : Refused("DataSetFilterValueInvalid", Described(dataSetName, filter).With("value", filter.Value).With("dataType", dataType.Name)
                .With("reason", value.FailureReason().Message), logger);
    }

    private static ResultDetails Described(string dataSetName, DataSetFilterConditionConfiguration filter) =>
        ResultDetails.Create("name", dataSetName, "ordinal", filter.Ordinal, "field", filter.FieldName).With("operator", filter.Operator);

    private static IGenericResult<IFilterNode> Refused(string code, ResultDetails details, ILogger logger) =>
        GenericResult<IFilterNode>.Failure(DataSetsResultCodes.ByName(code), logger, details);
}
