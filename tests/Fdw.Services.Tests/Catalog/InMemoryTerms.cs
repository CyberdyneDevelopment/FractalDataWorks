using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Configuration;
using Moq;

namespace Fdw.Services.Tests.Catalog;

/// <summary>
/// A glossary term provider that holds terms in memory and answers a <see cref="DomainQuery"/> the way the store would: it keeps the terms
/// the filter selects (starts with, contains, or), orders them by name, pages them, and counts when asked. It records the queries made and the
/// terms it handed back.
/// </summary>
internal sealed class InMemoryTerms
{
    private readonly List<GlossaryTermDomainConfiguration> _terms;

    public InMemoryTerms(IEnumerable<GlossaryTermDomainConfiguration> terms)
    {
        _terms = terms.ToList();
        Provider = new Mock<IGlossaryTermConfigurationProvider>();
        Provider
            .Setup(p => p.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainQuery query, CancellationToken _) => Answer(query));
    }

    public Mock<IGlossaryTermConfigurationProvider> Provider { get; }

    /// <summary>Gets the queries made, in order.</summary>
    public List<DomainQuery> Queries { get; } = [];

    /// <summary>Gets how many terms were handed back over all queries.</summary>
    public int TermsReturned { get; private set; }

    public static GlossaryTermDomainConfiguration Term(string name, string definition, string? standsFor = null, string owner = "Finance")
        => new() { Id = Guid.NewGuid(), Name = name, Definition = definition, StandsFor = standsFor, Owner = owner };

    private IGenericResult<ConfigurationPage<GlossaryTermDomainConfiguration>> Answer(DomainQuery query)
    {
        Queries.Add(query);
        var selected = _terms
            .Where(term => query.Filter?.Root is not { } root || Matches(root, term))
            .OrderBy(term => term.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(term => term.Id)
            .ToList();
        var page = selected.Skip(query.Skip).Take(query.Take).ToList();
        TermsReturned += page.Count;
        return GenericResult<ConfigurationPage<GlossaryTermDomainConfiguration>>.Success(new ConfigurationPage<GlossaryTermDomainConfiguration>(
            query.Skip, query.Take, selected.Count > query.Skip + query.Take, _ => page.ToAsyncEnumerable(), query.IncludeTotal ? selected.Count : null));
    }

    private static bool Matches(IFilterNode node, GlossaryTermDomainConfiguration term) => node switch
    {
        FilterGroup group => group.Nodes.Any(child => Matches(child, term)),
        FilterCondition condition => Matches(condition, (string?)typeof(GlossaryTermDomainConfiguration).GetProperty(condition.PropertyName)!.GetValue(term)),
        _ => throw new NotSupportedException($"The fake provider does not answer a {node.GetType().Name}"),
    };

    private static bool Matches(FilterCondition condition, string? value) => condition.Operator.Name switch
    {
        "StartsWith" => value is not null && value.StartsWith((string)condition.Value!, StringComparison.OrdinalIgnoreCase),
        "Contains" => value is not null && value.Contains((string)condition.Value!, StringComparison.OrdinalIgnoreCase),
        _ => throw new NotSupportedException($"The fake provider does not answer operator {condition.Operator.Name}"),
    };
}
