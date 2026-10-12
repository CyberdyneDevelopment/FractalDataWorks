using System.Linq;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Tests.Expressions;

/// <summary>
/// FDW-978: the result cache keys a query on its ordering's hash, and a list's ordering is chosen by
/// the caller, so two orderings with the same fields must be equal and hash alike.
/// </summary>
public class OrderingExpressionEqualityTests
{
    [Fact]
    public void TwoOrderingsOverTheSameFieldsInTheSameDirectionsAreEqualAndHashAlike()
    {
        var first = Ordering(("Name", "Ascending"), ("Id", "Ascending"));
        var second = Ordering(("Name", "Ascending"), ("Id", "Ascending"));

        first.Equals(second).ShouldBeTrue();
        first.GetHashCode().ShouldBe(second.GetHashCode());
    }

    [Fact]
    public void ADifferentDirectionOrderOrFieldMakesThemDiffer()
    {
        var baseline = Ordering(("Name", "Ascending"), ("Id", "Ascending"));

        baseline.Equals(Ordering(("Name", "Descending"), ("Id", "Ascending"))).ShouldBeFalse();
        baseline.Equals(Ordering(("Id", "Ascending"), ("Name", "Ascending"))).ShouldBeFalse();
        baseline.Equals(Ordering(("Name", "Ascending"))).ShouldBeFalse();
    }

    private static OrderingExpression Ordering(params (string Field, string Direction)[] fields)
        => new()
        {
            OrderedFields = fields
                .Select(f => (IOrderedField)new OrderedField { PropertyName = f.Field, Direction = SortDirections.ByName(f.Direction) })
                .ToList(),
        };
}
