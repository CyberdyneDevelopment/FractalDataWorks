using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;

namespace Fdw.Commands.Data.Tests;

/// <summary>
/// A write narrowed to a scope touches only the rows the scope keeps, and keeps everything else it states (FDW-5069): what lets a data set
/// with its own filters scope a write through it.
/// </summary>
public sealed class ScopeNarrowingTests
{
    private static FilterExpression Open() => new()
    {
        Root = new FilterCondition { PropertyName = "status", Operator = FilterOperators.ByName("Equal"), Value = "open" },
    };

    private static FilterExpression Quantity() => new()
    {
        Root = new FilterCondition { PropertyName = "qty", Operator = FilterOperators.ByName("GreaterThan"), Value = 3 },
    };

    [Fact]
    [Trait("Category", "DataSets.FDW5069")]
    public void AnUpdateNarrowedToAScopeAndsItWithItsOwnFilterAndKeepsTheRest()
    {
        var data = new Dictionary<string, object?> { ["qty"] = 9 };
        var update = new UpdateCommand<Dictionary<string, object?>>(data) { Filter = Quantity(), ReturnColumns = ["qty"] };

        var narrowed = update.NarrowedTo(Open()).ShouldBeOfType<UpdateCommand<Dictionary<string, object?>>>();

        narrowed.Data.ShouldBeSameAs(data);
        narrowed.ReturnColumns.ShouldBe(["qty"]);
        var group = narrowed.Filter.ShouldNotBeNull().Root.ShouldBeOfType<FilterGroup>();
        ReferenceEquals(group.Operator, LogicalOperator.And).ShouldBeTrue();
        group.Nodes.Cast<IFilterCondition>().Select(c => c.PropertyName).ShouldBe(["status", "qty"]);
    }

    [Fact]
    [Trait("Category", "DataSets.FDW5069")]
    public void AnUpdateWithNoFilterNarrowedToAScopeTakesTheScope()
    {
        var narrowed = (IFilterableCommand)new UpdateCommand<Dictionary<string, object?>>(new Dictionary<string, object?>()).NarrowedTo(Open());

        narrowed.Filter.ShouldNotBeNull().Root.ShouldBeAssignableTo<IFilterCondition>().ShouldNotBeNull().PropertyName.ShouldBe("status");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW5069")]
    public void ADeleteOfEveryRowNarrowedToAScopeDeletesTheScopesRows()
    {
        var narrowed = new DeleteCommand { AllRows = true }.NarrowedTo(Open()).ShouldBeOfType<DeleteCommand>();

        narrowed.AllRows.ShouldBeFalse();
        narrowed.Filter.ShouldNotBeNull().Root.ShouldBeAssignableTo<IFilterCondition>().ShouldNotBeNull().PropertyName.ShouldBe("status");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW5069")]
    public void AFilteredDeleteNarrowedToAScopeAndsBoth()
    {
        var narrowed = new DeleteCommand { Filter = Quantity() }.NarrowedTo(Open()).ShouldBeOfType<DeleteCommand>();

        narrowed.Filter.ShouldNotBeNull().Root.ShouldBeOfType<FilterGroup>().Nodes.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Category", "DataSets.FDW5069")]
    public void AnInsertExposesTheRowsItWrites()
    {
        var rows = new[] { new Dictionary<string, object?> { ["status"] = "open" }, new Dictionary<string, object?> { ["status"] = "closed" } };

        ((IRowInputCommand)new InsertCommand<Dictionary<string, object?>>(rows)).InputRows.Cast<object>().Count().ShouldBe(2);
    }
}
