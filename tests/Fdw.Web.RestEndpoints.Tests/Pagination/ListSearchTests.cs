using System;
using System.Linq;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Web.RestEndpoints.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.RestEndpoints.Tests.Pagination;

/// <summary>
/// FDW-1194: a list that is searched reads <c>?q=</c>, <c>?match=</c> and <c>?total=true</c> through <see cref="ListSearch"/>,
/// so what it finds and what it refuses is the same for every list.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Api")]
public sealed class ListSearchTests
{
    private static DefaultHttpContext Context(string query)
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString(query);
        return context;
    }

    private static ListSearch Parse(string query)
    {
        var search = ListSearch.Parse(Context(query), NullLogger.Instance);
        search.IsSuccess.ShouldBeTrue();
        return search.Value!;
    }

    [Fact]
    public void NoQuerySelectsEveryMemberAndAsksForNoTotal()
    {
        var search = Parse(string.Empty);

        search.Text.ShouldBeNull();
        search.IncludeTotal.ShouldBeFalse();
        search.Match.Wire.ShouldBe("contains");
        search.Filter("Name", "Id").ShouldBeNull();
    }

    // A blank q is not a search for blank text.
    [Theory]
    [InlineData("?q=")]
    [InlineData("?q=%20%20")]
    public void ABlankTextSelectsEveryMember(string query)
    {
        var search = Parse(query);

        search.Text.ShouldBeNull();
        search.Filter("Name", "Id").ShouldBeNull();
    }

    [Theory]
    [InlineData("?q=brew", "contains", "Contains")]
    [InlineData("?q=brew&match=contains", "contains", "Contains")]
    [InlineData("?q=brew&match=startsWith", "startsWith", "StartsWith")]
    [InlineData("?q=brew&match=exact", "exact", "Equal")]
    public void TheMatchChoosesTheFilterOperatorOfTheFilter(string query, string wire, string operatorName)
    {
        var search = Parse(query);

        search.Text.ShouldBe("brew");
        search.Match.Wire.ShouldBe(wire);
        var condition = search.Filter("Name", "Id")!.Root.ShouldBeOfType<FilterCondition>();
        condition.PropertyName.ShouldBe("Name");
        condition.Operator.Name.ShouldBe(operatorName);
        condition.Value.ShouldBe("brew");
    }

    // The word is matched exactly: a match that names none is refused, never given the default.
    [Theory]
    [InlineData("?q=a&match=Contains")]
    [InlineData("?q=a&match=like")]
    [InlineData("?q=a&match=")]
    public void AMatchThatNamesNoWayIsRefused(string query)
    {
        ListSearch.Parse(Context(query), NullLogger.Instance).IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData("?total=true", true)]
    [InlineData("?total=false", false)]
    [InlineData("", false)]
    public void TotalIsAskedForOnlyWhenSaidSo(string query, bool expected)
    {
        Parse(query).IncludeTotal.ShouldBe(expected);
    }

    [Fact]
    public void ATotalThatIsNeitherTrueNorFalseIsRefused()
    {
        ListSearch.Parse(Context("?total=maybe"), NullLogger.Instance).IsFailure.ShouldBeTrue();
    }

    private static readonly Guid First = Guid.Parse("0b8b6a4e-6f1d-4c1e-9d3b-7f5a1c2d3e4f");
    private static readonly Guid Second = Guid.Parse("1c9c7b5f-7a2e-4d2f-8e4c-8a6b2d3e4f50");

    [Fact]
    public void NoIdsKeepsEveryMember()
    {
        Parse("?q=brew").Ids.ShouldBeEmpty();
    }

    // Repeated and comma separated give the same identifiers, in the order named, each once.
    [Theory]
    [InlineData("?id=0b8b6a4e-6f1d-4c1e-9d3b-7f5a1c2d3e4f&id=1c9c7b5f-7a2e-4d2f-8e4c-8a6b2d3e4f50")]
    [InlineData("?id=0b8b6a4e-6f1d-4c1e-9d3b-7f5a1c2d3e4f,1c9c7b5f-7a2e-4d2f-8e4c-8a6b2d3e4f50")]
    [InlineData("?id=0b8b6a4e-6f1d-4c1e-9d3b-7f5a1c2d3e4f,%201c9c7b5f-7a2e-4d2f-8e4c-8a6b2d3e4f50&id=0b8b6a4e-6f1d-4c1e-9d3b-7f5a1c2d3e4f")]
    public void IdsMayBeRepeatedOrCommaSeparated(string query)
    {
        Parse(query).Ids.ShouldBe([First, Second]);
    }

    [Theory]
    [InlineData("?id=not-a-guid")]
    [InlineData("?id=0b8b6a4e-6f1d-4c1e-9d3b-7f5a1c2d3e4f,oops")]
    [InlineData("?id=")]
    [InlineData("?id=0b8b6a4e-6f1d-4c1e-9d3b-7f5a1c2d3e4f,,1c9c7b5f-7a2e-4d2f-8e4c-8a6b2d3e4f50")]
    public void AnIdThatIsNotAGuidIsRefused(string query)
    {
        ListSearch.Parse(Context(query), NullLogger.Instance).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void TheLimitIsStatedAndOneOverItIsRefusedNotCutShort()
    {
        ListSearch.MaxIds.ShouldBe(100);

        var atTheLimit = string.Join("&", Enumerable.Range(0, 100).Select(_ => $"id={Guid.NewGuid()}"));
        var overTheLimit = atTheLimit + $"&id={Guid.NewGuid()}";

        Parse("?" + atTheLimit).Ids.Count.ShouldBe(100);
        ListSearch.Parse(Context("?" + overTheLimit), NullLogger.Instance).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void IdsAloneAreAnInFilterOnTheIdentifierMember()
    {
        var condition = Parse($"?id={First},{Second}").Filter("Name", "Id")!.Root.ShouldBeOfType<FilterCondition>();

        condition.PropertyName.ShouldBe("Id");
        condition.Operator.Name.ShouldBe("In");
        condition.Value.ShouldBeOfType<System.Collections.Generic.List<object?>>().Cast<Guid>().ShouldBe([First, Second]);
    }

    [Fact]
    public void IdsAndTextAreAnAnd()
    {
        var group = Parse($"?q=brew&id={First}").Filter("Name", "Id")!.Root.ShouldBeOfType<FilterGroup>();

        group.Operator.Name.ShouldBe("And");
        group.Nodes.Count.ShouldBe(2);
        group.Nodes.OfType<FilterCondition>().Select(condition => condition.PropertyName).ShouldBe(["Name", "Id"]);
    }

    [Fact]
    public void WithNoTextEveryValueIsSelected()
    {
        Parse(string.Empty).Selects("anything", null).ShouldBeTrue();
    }

    [Theory]
    [InlineData("?q=BREW", "Brewery", true)]
    [InlineData("?q=brew", "A Brewery", true)]
    [InlineData("?q=brew", "Beer", false)]
    [InlineData("?q=brew&match=startsWith", "Brewery", true)]
    [InlineData("?q=brew&match=startsWith", "A Brewery", false)]
    [InlineData("?q=brewery&match=exact", "BREWERY", true)]
    [InlineData("?q=brew&match=exact", "Brewery", false)]
    public void ValuesAreComparedWithoutRegardToCaseInTheWayTheMatchSays(string query, string value, bool selected)
    {
        Parse(query).Selects(value).ShouldBe(selected);
    }

    [Fact]
    public void AnyOfTheValuesMaySelectAndAnAbsentValueIsSkipped()
    {
        var search = Parse("?q=weekly");

        search.Selects("Name", null, "Loaded weekly from the feed").ShouldBeTrue();
        search.Selects("Name", null).ShouldBeFalse();
    }
}
