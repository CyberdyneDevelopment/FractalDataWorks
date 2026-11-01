
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;
using static Fdw.Services.Tests.Catalog.InMemoryTerms;

namespace Fdw.Services.Tests.Catalog;

/// <summary>
/// FDW-2312: the glossary is searched at the term provider, a ranked window at a time and counted, never read whole. The closest terms are
/// the ones the rule would pick over the whole glossary, and the terms read do not grow with the glossary.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public class GlossaryTermSearchTests
{
    private static (GlossaryTermSearch Search, InMemoryTerms Terms) Over(List<Fdw.Services.Quality.Configuration.GlossaryTermDomainConfiguration> terms)
    {
        var held = new InMemoryTerms(terms);
        return (new GlossaryTermSearch(held.Provider.Object, NullLogger.Instance), held);
    }

    private static List<Fdw.Services.Quality.Configuration.GlossaryTermDomainConfiguration> Glossary(int size)
    {
        var terms = Enumerable.Range(0, size).Select(i => Term($"term{i % 97}_{i}", i % 5 == 0 ? $"the amount of item {i % 31}" : "plain")).ToList();
        terms.Add(Term("Amount", "How much"));
        terms.Add(Term("AmountDue", "What is owed"));
        terms.Add(Term("TotalAmount", "Sum"));
        terms.Add(Term("ARR", "Annual recurring revenue", "amount recurring"));
        terms.Add(Term("Net", "The amount after tax"));
        return terms;
    }

    [Theory]
    [InlineData("amount", 1)]
    [InlineData("amount", 4)]
    [InlineData("amount", 200)]
    [InlineData("term1_", 5)]
    [InlineData("zzz", 5)]
    public async Task TheClosestTermsAreTheOnesTheRuleWouldPickOverTheWholeGlossary(string text, int window)
    {
        var terms = Glossary(300);
        var (search, _) = Over(terms);

        var found = await search.Search(text, window, TestContext.Current.CancellationToken);

        found.IsSuccess.ShouldBeTrue(found.CurrentMessage);
        var expected = terms
            .Select(term => (term.Name, Rank: SearchRank.Of(term.Name, term.StandsFor is { Length: > 0 } s ? s + ". " + term.Definition : term.Definition, text)))
            .Where(candidate => candidate.Rank != SearchRank.NoMatch)
            .OrderBy(candidate => candidate.Rank)
            .ThenBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase)
            .Take(window)
            .Select(candidate => candidate.Name)
            .ToList();
        found.Value!.Hits.Select(hit => hit.Name).ToList().ShouldBe(expected);
    }

    [Fact]
    public async Task ATermIsDescribedByWhatItStandsForThenItsDefinitionAndKeepsItsOwner()
    {
        var (search, _) = Over([Term("ARR", "Annual recurring revenue", "Annual recurring revenue figure", "Finance")]);

        var found = await search.Search("figure", 5, TestContext.Current.CancellationToken);

        var hit = found.Value!.Hits.ShouldHaveSingleItem();
        hit.Description.ShouldBe("Annual recurring revenue figure. Annual recurring revenue");
        hit.Owner.ShouldBe("Finance");
        hit.Rank.ShouldBe(3);
    }

    [Fact]
    public async Task TheNumberOfMatchesIsCountedAtTheProviderNotFromTheWindow()
    {
        var terms = Glossary(300);
        var (search, _) = Over(terms);

        var found = await search.Search("amount", 3, TestContext.Current.CancellationToken);

        found.Value!.Hits.Count.ShouldBe(3);
        found.Value.Matches.ShouldBe(terms.Count(term => term.Name.Contains("amount", StringComparison.OrdinalIgnoreCase)
            || term.Definition.Contains("amount", StringComparison.OrdinalIgnoreCase)
            || (term.StandsFor?.Contains("amount", StringComparison.OrdinalIgnoreCase) ?? false)));
    }

    [Fact]
    public async Task ABrowseListsTheFirstTermsByNameAndCountsAll()
    {
        var terms = Glossary(50);
        var (search, _) = Over(terms);

        var found = await search.Search(string.Empty, 4, TestContext.Current.CancellationToken);

        found.Value!.Hits.Select(hit => hit.Name).ShouldBe(terms.Select(term => term.Name).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).Take(4));
        found.Value.Matches.ShouldBe(terms.Count);
    }

    [Fact]
    public async Task TheTermsReadDoNotGrowWithTheGlossary()
    {
        var (smallSearch, small) = Over(Glossary(400));
        var (largeSearch, large) = Over(Glossary(20000));

        await smallSearch.Search("amount", 25, TestContext.Current.CancellationToken);
        await largeSearch.Search("amount", 25, TestContext.Current.CancellationToken);

        large.Queries.Count.ShouldBe(small.Queries.Count);
        large.TermsReturned.ShouldBe(small.TermsReturned);
        large.TermsReturned.ShouldBeLessThanOrEqualTo(5 * 25);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public async Task AWindowOutsideTheLimitsIsRefusedBeforeAnythingIsRead(int window)
    {
        var (search, terms) = Over(Glossary(10));

        var found = await search.Search("amount", window, TestContext.Current.CancellationToken);

        found.IsFailure.ShouldBeTrue();
        terms.Queries.ShouldBeEmpty();
    }

    [Fact]
    public async Task ATermIsPlacedAsItIsBecauseItSitsUnderNothing()
    {
        var (search, _) = Over(Glossary(10));
        var found = await search.Search("amount", 3, TestContext.Current.CancellationToken);

        var placed = await search.Place(found.Value!.Hits, TestContext.Current.CancellationToken);

        placed.Value!.Count.ShouldBe(found.Value.Hits.Count);
    }
}
