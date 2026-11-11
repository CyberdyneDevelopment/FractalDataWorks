using System.Linq;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// A data set's own filter conditions (FDW-4997): each is checked rule by rule and built into one typed filter, ANDed in ordinal order;
/// a condition that breaks a rule is refused with its own code, never dropped.
/// </summary>
public sealed class DataSetFiltersTests
{
    private static SimpleDataSetConfiguration DataSet(params DataSetFilterConditionConfiguration[] filters) => new()
    {
        Name = "Orders",
        Implementation = "Simple",
        Fields =
        [
            new DataSetFieldConfiguration { Name = "Status", Ordinal = 0 },
            new DataSetFieldConfiguration { Name = "Quantity", Ordinal = 1 },
            new DataSetFieldConfiguration { Name = "ShippedOn", Ordinal = 2 },
            new DataSetFieldConfiguration { Name = "Label", Ordinal = 3, CalculationName = "Concat" },
        ],
        Filters = [.. filters],
    };

    private static DataSetFilterConditionConfiguration Filter(string field, string op, string? value, string dataType, int ordinal = 0) =>
        new() { FieldName = field, Operator = op, Value = value, DataType = dataType, Ordinal = ordinal };

    [Fact]
    [Trait("Category", "DataSets.FDW4997")]
    public void ADataSetWithNoConditionHasNoFilter()
    {
        var result = DataSetFilters.Expression(DataSet(), NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Category", "DataSets.FDW4997")]
    public void ConditionsAreAndedInOrdinalOrderWithTypedValues()
    {
        var result = DataSetFilters.Expression(
            DataSet(Filter("Quantity", "GreaterThan", "5", "Int32", ordinal: 1), Filter("Status", "Equal", "open", "String", ordinal: 0)),
            NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var group = result.Value.ShouldNotBeNull().Root.ShouldBeOfType<FilterGroup>();
        ReferenceEquals(group.Operator, LogicalOperator.And).ShouldBeTrue();
        var conditions = group.Nodes.Cast<IFilterCondition>().ToList();
        conditions.Select(c => c.PropertyName).ShouldBe(["Status", "Quantity"]);
        conditions[0].Value.ShouldBe("open");
        conditions[1].Value.ShouldBe(5);
        conditions[1].Operator.Name.ShouldBe("GreaterThan");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW4997")]
    public void AnOperatorThatTakesNoValueIsBuiltWithoutOne()
    {
        var result = DataSetFilters.Expression(DataSet(Filter("ShippedOn", "IsNull", null, "DateTime")), NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var condition = result.Value.ShouldNotBeNull().Root.ShouldBeAssignableTo<IFilterCondition>().ShouldNotBeNull();
        condition.Operator.Name.ShouldBe("IsNull");
        condition.Value.ShouldBeNull();
    }

    [Theory]
    [Trait("Category", "DataSets.FDW4997")]
    [InlineData("Region", "Equal", "north", "String", "DATASETS-20022")]
    [InlineData("Label", "Equal", "x", "String", "DATASETS-20022")]
    [InlineData("Status", "Equals", "open", "String", "DATASETS-20023")]
    [InlineData("Status", "Equal", "open", "Text", "DATASETS-20024")]
    [InlineData("Status", "Equal", null, "String", "DATASETS-20025")]
    [InlineData("ShippedOn", "IsNull", "2026-01-01", "DateTime", "DATASETS-20026")]
    [InlineData("Quantity", "Equal", "many", "Int32", "DATASETS-20027")]
    public void AConditionThatBreaksARuleIsRefusedWithItsOwnCode(string field, string op, string? value, string dataType, string code)
    {
        var result = DataSetFilters.Expression(DataSet(Filter(field, op, value, dataType)), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Code!.Code.ShouldBe(code);
    }

    [Fact]
    [Trait("Category", "DataSets.FDW4997")]
    public void ADeletedConditionIsNotPartOfTheFilter()
    {
        var deleted = Filter("Quantity", "Equal", "many", "Int32");
        deleted.IsDeleted = true;

        var result = DataSetFilters.Expression(DataSet(deleted), NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }
}
