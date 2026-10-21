using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Catalog;

/// <summary>FDW-1682: the glossary list is narrowed by the search text, by acronym and by approved state.</summary>
[Collection(nameof(ServicesTestCollection))]
public class GlossaryTermFilterTests
{
    private static IGlossaryTermImplementationConfiguration Term(string name, string definition, string? standsFor = null, bool? approved = null)
    {
        var term = new Mock<IGlossaryTermImplementationConfiguration>();
        term.SetupGet(t => t.Name).Returns(name);
        term.SetupGet(t => t.Definition).Returns(definition);
        term.SetupGet(t => t.StandsFor).Returns(standsFor);
        term.SetupGet(t => t.IsApproved).Returns(approved);
        return term.Object;
    }

    private static readonly IGlossaryTermImplementationConfiguration[] Terms =
    [
        Term("MTD", "Revenue so far this month", "Month to date", true),
        Term("YTD", "Revenue so far this year", "Year to date", false),
        Term("Churn", "Customers lost in a period"),
    ];

    private static string[] Names(string? query = null, string? acronym = null, string? approved = null) =>
        [.. GlossaryTermFilter.Apply(Terms, query, acronym, approved, NullLogger.Instance).Value!.Select(term => term.Name)];

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void NoFilterListsEveryTerm()
    {
        Names().ShouldBe(["MTD", "YTD", "Churn"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void TheAcronymFilterListsTheAcronymsOrTheTermsThatAreNot()
    {
        Names(acronym: "true").ShouldBe(["MTD", "YTD"]);
        Names(acronym: "false").ShouldBe(["Churn"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void TheApprovedFilterListsOnlyAcronymsInThatState()
    {
        Names(approved: "true").ShouldBe(["MTD"]);
        Names(approved: "false").ShouldBe(["YTD"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void TheSearchTextMatchesWhatAnAcronymStandsForIgnoringCase()
    {
        Names(query: "MONTH TO").ShouldBe(["MTD"]);
        Names(query: "customers").ShouldBe(["Churn"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void AFilterThatIsNotTrueOrFalseIsRefusedNotIgnored()
    {
        var refused = GlossaryTermFilter.Apply(Terms, null, "yes", null, NullLogger.Instance);

        refused.IsFailure.ShouldBeTrue();
        refused.CurrentMessage.ShouldNotBeNull();
    }
}
