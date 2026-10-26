using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Services.Quality.Configuration;
using Fdw.Web.RestEndpoints.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Catalog;

/// <summary>
/// FDW-1682, FDW-2336: the glossary list is narrowed by the search text, by acronym, by approved state and by category, as a filter on
/// the fields of the term's domain row, so the store does the narrowing.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public class GlossaryTermFilterTests
{
    private static ListSearch Search(string query = "")
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString(query);
        return ListSearch.Parse(context, NullLogger.Instance).Value!;
    }

    private static IFilterNode? Root(string search = "", string? acronym = null, string? approved = null, IReadOnlyCollection<Guid>? categories = null)
    {
        var built = GlossaryTermFilter.Build(Search(search), acronym, approved, categories, "Finance", NullLogger.Instance);
        built.IsSuccess.ShouldBeTrue();
        return built.Value?.Root;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void NoFilterNarrowsNothing()
    {
        Root().ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void TheAcronymFilterIsAConditionOnWhatTheTermStandsFor()
    {
        var acronyms = Root(acronym: "true").ShouldBeOfType<FilterCondition>();
        var others = Root(acronym: "false").ShouldBeOfType<FilterCondition>();

        acronyms.PropertyName.ShouldBe("StandsFor");
        acronyms.Operator.Name.ShouldBe("IsNotNull");
        others.PropertyName.ShouldBe("StandsFor");
        others.Operator.Name.ShouldBe("IsNull");
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void TheApprovedFilterIsAnAcronymInThatState(string approved, bool expected)
    {
        var group = Root(approved: approved).ShouldBeOfType<FilterGroup>();

        group.Operator.Name.ShouldBe("And");
        var conditions = group.Nodes.Cast<FilterCondition>().ToList();
        conditions.Select(condition => (condition.PropertyName, condition.Operator.Name))
            .ShouldBe([("StandsFor", "IsNotNull"), ("IsApproved", "Equal")]);
        conditions[1].Value.ShouldBe(expected);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void TheSearchTextIsFoundInTheNameTheDefinitionAndWhatAnAcronymStandsFor()
    {
        var group = Root(search: "?q=month").ShouldBeOfType<FilterGroup>();

        group.Operator.Name.ShouldBe("Or");
        var conditions = group.Nodes.Cast<FilterCondition>().ToList();
        conditions.Select(condition => condition.PropertyName).ShouldBe(["Name", "Definition", "StandsFor"]);
        conditions.ShouldAllBe(condition => condition.Operator.Name == "Contains" && (string?)condition.Value == "month");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void TheMatchTheRequestNamesAppliesToEveryProperty()
    {
        var group = Root(search: "?q=mtd&match=startsWith").ShouldBeOfType<FilterGroup>();

        group.Nodes.Cast<FilterCondition>().ShouldAllBe(condition => condition.Operator.Name == "StartsWith");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ACategoryIsAConditionOnTheSetOfCategoriesUnderIt()
    {
        var under = new[] { Guid.NewGuid(), Guid.NewGuid() };

        var condition = Root(categories: under).ShouldBeOfType<FilterCondition>();

        condition.PropertyName.ShouldBe("CategoryId");
        condition.Operator.Name.ShouldBe("In");
        ((IEnumerable<object?>)condition.Value!).ShouldBe(under.Cast<object?>());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void SeveralFiltersAreAnAnd()
    {
        var group = Root(search: "?q=month", acronym: "true", categories: [Guid.NewGuid()]).ShouldBeOfType<FilterGroup>();

        group.Operator.Name.ShouldBe("And");
        group.Nodes.Count.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void AFilterThatIsNotTrueOrFalseIsRefusedNotIgnored()
    {
        var refused = GlossaryTermFilter.Build(Search(), "yes", null, null, null, NullLogger.Instance);
        var refusedApproved = GlossaryTermFilter.Build(Search(), null, "1", null, null, NullLogger.Instance);

        refused.IsFailure.ShouldBeTrue();
        refused.CurrentMessage.ShouldNotBeNull();
        refusedApproved.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ACategoryWithMoreCategoriesUnderItThanAListCarriesIsRefusedNamingIt()
    {
        var tooMany = Enumerable.Range(0, GlossaryTermFilter.MaxCategories + 1).Select(_ => Guid.NewGuid()).ToList();

        var refused = GlossaryTermFilter.Build(Search(), null, null, tooMany, "Finance", NullLogger.Instance);

        refused.IsFailure.ShouldBeTrue();
        refused.CurrentMessage.ShouldNotBeNull().ShouldContain("Finance");
    }
}
