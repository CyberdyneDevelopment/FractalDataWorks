using System.Linq;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>FDW-3638, FDW-3873: the list and the lookup of data sets narrow by <c>?kind=</c> through one reading.</summary>
public sealed class RequestedDataSetKindsTests
{
    private static HttpRequest Request(string query)
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString(query);
        return context.Request;
    }

    [Fact]
    public void WithNoKindNothingIsNarrowed()
    {
        RequestedDataSetKinds.Of(Request(string.Empty), NullLogger.Instance).Value.ShouldBeEmpty();
    }

    [Fact]
    public void KindsAreReadFromACommaListAndFromRepeatedParametersEachOnce()
    {
        var kinds = RequestedDataSetKinds.Of(Request("?kind=Variation,Simple&kind=variation"), NullLogger.Instance);

        kinds.IsSuccess.ShouldBeTrue();
        kinds.Value!.Count.ShouldBe(2);
        RequestedDataSetKinds.ImplementationNames(kinds.Value).ShouldBe(["Variation", "Simple"]);
    }

    [Fact]
    public void AKindDataSetTypesDoesNotHoldIsRefusedAndNeverIgnored()
    {
        RequestedDataSetKinds.Of(Request("?kind=Variation,Planet"), NullLogger.Instance).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void NarrowingKeepsTheCallersFilterAndAddsTheKinds()
    {
        var caller = new FilterCondition { PropertyName = "Name", Operator = FilterOperators.ByName("Equal"), Value = "x" };
        var query = new DomainQuery(new FilterExpression { Root = caller }, null, 0, 10, false);

        var narrowed = RequestedDataSetKinds.Narrow(query, ["Variation"]);

        var group = narrowed.Filter!.Root.ShouldBeOfType<FilterGroup>();
        group.Nodes.Count.ShouldBe(2);
        group.Nodes.ShouldContain(caller);
        group.Nodes.OfType<FilterCondition>().Single(c => c.PropertyName == "Implementation").Value.ShouldNotBeNull();
    }
}
