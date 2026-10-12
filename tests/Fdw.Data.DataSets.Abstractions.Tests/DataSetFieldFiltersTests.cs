using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Schema;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Abstractions.Tests;

/// <summary>FDW-1102: the one answer to which filter operators a data set field takes.</summary>
public sealed class DataSetFieldFiltersTests
{
    [Fact]
    public void AFieldThatIsNotFilterableTakesNoOperator()
        => DataSetFieldFilters.Operators(false, DatumTypes.Attribute.Id, "String").ShouldBeEmpty();

    [Fact]
    public void AFieldNobodyHasTypedTakesNoOperator()
        => DataSetFieldFilters.Operators(true, DatumTypes.NotYetTyped.Id, "String").ShouldBeEmpty();

    [Fact]
    public void AFieldWhoseDataTypeIsNotAKnownNameTakesNoOperator()
        => DataSetFieldFilters.Operators(true, DatumTypes.Attribute.Id, "Some.Custom.Thing").ShouldBeEmpty();

    [Fact]
    public void ATextFieldTakesTheTextOperatorsAndNoRange()
    {
        var operators = DataSetFieldFilters.Operators(true, DatumTypes.Attribute.Id, "String");

        operators.ShouldContain("Contains");
        operators.ShouldContain("NotIn");
        operators.ShouldNotContain("GreaterThan");
        operators.ShouldNotContain("Between");
    }

    [Fact]
    public void AWholeNumberMeasureTakesRangeBetweenAndNotIn()
    {
        var operators = DataSetFieldFilters.Operators(true, DatumTypes.Measure.Id, "Int32");

        operators.ShouldContain("Between");
        operators.ShouldContain("NotIn");
        operators.ShouldContain("GreaterThanOrEqual");
    }

    [Fact]
    public void ADecimalMeasureTakesBetweenButNotNotIn()
    {
        var operators = DataSetFieldFilters.Operators(true, DatumTypes.Measure.Id, "Decimal");

        operators.ShouldContain("Between");
        operators.ShouldNotContain("NotIn");
    }

    [Fact]
    public void ADateTakesBetweenAsItsStartDateAndEndDateRangeButNotNotIn()
    {
        var operators = DataSetFieldFilters.Operators(true, DatumTypes.Date.Id, "DateTimeOffset");

        operators.ShouldContain("Between");
        operators.ShouldContain("LessThan");
        operators.ShouldNotContain("NotIn");
    }

    [Fact]
    public void ABooleanHasNoRangeEvenUnderADatumTypeThatAllowsOne()
    {
        var operators = DataSetFieldFilters.Operators(true, DatumTypes.Measure.Id, "Bool");

        operators.ShouldContain("Equal");
        operators.ShouldNotContain("GreaterThan");
        operators.ShouldNotContain("Between");
    }

    [Fact]
    public void WhatAFieldTakesIsNeverMoreThanItsDatumTypeAllows()
    {
        foreach (var datum in new DatumTypeBase[] { DatumTypes.Identifier, DatumTypes.Attribute, DatumTypes.Categorical, DatumTypes.Date, DatumTypes.Measure })
        {
            var taken = DataSetFieldFilters.Operators(true, datum.Id, "Int32");

            taken.All(name => datum.AllowedOperators.Contains(name)).ShouldBeTrue();
        }
    }

    [Fact]
    public void TheFieldOverloadAnswersLikeTheOneItReads()
    {
        var field = new DataSetFieldConfiguration { Name = "Units", TypeName = "Int32", IsFilterable = true, DatumTypeId = DatumTypes.Measure.Id };

        DataSetFieldFilters.Operators(field).ShouldBe(DataSetFieldFilters.Operators(true, DatumTypes.Measure.Id, "Int32"));
    }
}
