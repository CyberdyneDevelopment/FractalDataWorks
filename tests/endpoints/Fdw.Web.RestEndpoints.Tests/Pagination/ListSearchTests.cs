using System;
using System.Linq;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.RestEndpoints.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.Web.RestEndpoints.Tests.Pagination;

/// <summary>
/// FDW-1194: a list that is searched reads <c>?q=</c>, <c>?match=</c> and <c>?total=true</c> through <see cref="ListSearch"/>,
/// so what it finds and what it refuses is the same for every list.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Api")]
public sealed class ListSearchTests
{
    // The most identifiers one call states is the keys one statement carries (FDW-7106); a small number here, so a test can go past it.
    private const int KeysPerStatement = 25;

    private static DefaultHttpContext Context(string query) => Context(query, KeysPerStatement);

    private static DefaultHttpContext Context(string query, int keysPerStatement)
    {
        var gateways = new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict);
        gateways.SetupGet(g => g.KeyChunkSize).Returns(keysPerStatement);
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddSingleton(gateways.Object).BuildServiceProvider(),
        };
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
    public void TheLimitIsTheKeysOneStatementCarriesAndOneOverItIsRefusedNotCutShort()
    {
        var atTheLimit = string.Join("&", Enumerable.Range(0, KeysPerStatement).Select(_ => $"id={Guid.NewGuid()}"));
        var overTheLimit = atTheLimit + $"&id={Guid.NewGuid()}";

        Parse("?" + atTheLimit).Ids.Count.ShouldBe(KeysPerStatement);
        var refused = ListSearch.Parse(Context("?" + overTheLimit), NullLogger.Instance);
        refused.IsFailure.ShouldBeTrue();
        refused.CurrentMessage!.ShouldContain($"at most {KeysPerStatement} identifiers");
    }

    [Fact]
    public void AHostThatCarriesMoreKeysTakesMoreIdentifiers()
    {
        var many = string.Join("&", Enumerable.Range(0, KeysPerStatement * 4).Select(_ => $"id={Guid.NewGuid()}"));

        ListSearch.Parse(Context("?" + many, KeysPerStatement * 4), NullLogger.Instance).Value!.Ids.Count.ShouldBe(KeysPerStatement * 4);
    }

    [Fact]
    public void IdentifiersWithNoConfigurationGatewayAreRefusedAndARequestWithoutThemNeedsNone()
    {
        var bare = new DefaultHttpContext { RequestServices = new ServiceCollection().BuildServiceProvider() };
        bare.Request.QueryString = new QueryString($"?id={First}");
        var plain = new DefaultHttpContext { RequestServices = new ServiceCollection().BuildServiceProvider() };
        plain.Request.QueryString = new QueryString("?q=brew");

        var refused = ListSearch.Parse(bare, NullLogger.Instance);
        refused.IsFailure.ShouldBeTrue();
        refused.Messages.ShouldContain(m => m.Code == "RESTENDPOINTS-61005");
        ListSearch.Parse(plain, NullLogger.Instance).IsSuccess.ShouldBeTrue();
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
