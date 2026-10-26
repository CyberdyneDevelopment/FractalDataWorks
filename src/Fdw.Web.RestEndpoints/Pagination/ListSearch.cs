using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.RestEndpoints.Pagination;

/// <summary>
/// What a list request asks of a list beyond its page: text to find in names, how to compare it, and whether to count.
/// </summary>
/// <remarks>
/// Read from <c>?q=</c>, <c>?match=</c>, <c>?id=</c> and <c>?total=true</c>. An absent or blank <c>q</c> selects every
/// member; a blank one is not a search for blank. <c>match</c> names a way through <see cref="ListMatches"/> and one that
/// names none is refused, never given the default. <c>id</c> keeps only the members with those identifiers, and combines
/// with <c>q</c> as an AND. It may be repeated (<c>?id=a&amp;id=b</c>) or comma separated (<c>?id=a,b</c>); an identifier
/// that is not a GUID is refused naming it, and more than <see cref="MaxIds"/> identifiers in one call is refused, never
/// cut short; an identifier no member has simply does not appear. <c>total</c> is <c>true</c> or <c>false</c>; absent means
/// not asked, and a page that was not asked for a total carries none, so an absent total never reads as zero.
/// </remarks>
public sealed class ListSearch
{
    /// <summary>Gets the most identifiers one call may name with <c>?id=</c>.</summary>
    public const int MaxIds = 100;

    private ListSearch(string? text, IListMatch match, IReadOnlyList<Guid> ids, bool includeTotal)
    {
        Text = text;
        Match = match;
        Ids = ids;
        IncludeTotal = includeTotal;
    }

    /// <summary>Gets the text to find in names, or nothing to select every member.</summary>
    public string? Text { get; }

    /// <summary>Gets how <see cref="Text"/> is compared with a name.</summary>
    public IListMatch Match { get; }

    /// <summary>Gets the identifiers to keep, or none to keep every member.</summary>
    public IReadOnlyList<Guid> Ids { get; }

    /// <summary>Gets whether the page carries how many members the search selects in all.</summary>
    public bool IncludeTotal { get; }

    /// <summary>Gets a search that selects every member and asks for no total.</summary>
    public static ListSearch None => new(null, ListMatches.ByWire("contains"), [], false);

    /// <summary>Builds the filter that selects the members the search keeps.</summary>
    /// <param name="nameProperty">The member of the record the text is compared with, such as its name.</param>
    /// <param name="idProperty">The member of the record that holds its identifier.</param>
    /// <returns>The filter -- the text and the identifiers together when both were given -- or nothing when the search keeps every member.</returns>
    public IFilterExpression? Filter(string nameProperty, string idProperty) => Filter([nameProperty], idProperty);

    /// <summary>Builds the filter that selects the members the search keeps, finding the text in any of several properties.</summary>
    /// <param name="textProperties">The members of the record the text is compared with; a member is kept when the text is found in any of them.</param>
    /// <param name="idProperty">The member of the record that holds its identifier.</param>
    /// <returns>The filter -- the text and the identifiers together when both were given -- or nothing when the search keeps every member.</returns>
    /// <remarks>Each property is compared in the way <c>?match=</c> names, so a list that searches a name and a definition matches both alike.</remarks>
    public IFilterExpression? Filter(IReadOnlyList<string> textProperties, string idProperty)
    {
        var nodes = new List<IFilterNode>();
        if (Text is not null)
            nodes.Add(TextMatch(textProperties));

        if (Ids.Count > 0)
            nodes.Add(new FilterCondition
            {
                PropertyName = idProperty,
                Operator = FilterOperators.ByName("In"),
                Value = Ids.Cast<object?>().ToList(),
            });

        return nodes.Count switch
        {
            0 => null,
            1 => new FilterExpression { Root = nodes[0] },
            _ => new FilterExpression { Root = new FilterGroup { Operator = LogicalOperator.And, Nodes = nodes } },
        };
    }

    // One property is a condition; several are a group of which any member may hold the text.
    private IFilterNode TextMatch(IReadOnlyList<string> properties)
    {
        var conditions = properties
            .Select(property => (IFilterNode)new FilterCondition { PropertyName = property, Operator = Match.Operator, Value = Text })
            .ToList();
        return conditions.Count == 1
            ? conditions[0]
            : new FilterGroup { Operator = LogicalOperator.Or, Nodes = conditions };
    }

    /// <summary>Reads the search a list request asks for.</summary>
    /// <param name="context">The current request.</param>
    /// <param name="logger">The calling endpoint's logger.</param>
    /// <returns>The search; a failure when <c>match</c> names no way to compare, an <c>id</c> is not a GUID or there are more than <see cref="MaxIds"/>, or <c>total</c> is not true or false.</returns>
    public static IGenericResult<ListSearch> Parse(HttpContext context, ILogger logger)
    {
        var query = context.Request.Query;

        var text = query.TryGetValue("q", out var asked) && !string.IsNullOrWhiteSpace(asked.ToString())
            ? asked.ToString()
            : null;

        var match = ReadMatch(query, logger);
        if (match.IsFailure)
            return match.ToNewResult<ListSearch>();

        var ids = ReadIds(query, logger);
        if (ids.IsFailure)
            return ids.ToNewResult<ListSearch>();

        var includeTotal = QueryFlag.Read(context, "total", logger);
        return includeTotal.IsFailure
            ? includeTotal.ToNewResult<ListSearch>()
            : GenericResult<ListSearch>.Success(new ListSearch(text, match.Value!, ids.Value!, includeTotal.Value));
    }

    // The way ?match= names, or the way the list documents when the request does not name one; a word that names none is refused.
    private static IGenericResult<IListMatch> ReadMatch(IQueryCollection query, ILogger logger)
    {
        if (!query.TryGetValue("match", out var word))
            return GenericResult<IListMatch>.Success(ListMatches.ByWire("contains"));

        var found = ListMatches.ByWire(word.ToString());
        return ReferenceEquals(found, ListMatches.NotFound)
            ? GenericResult<IListMatch>.Failure(EndpointLogger.PaginationValueInvalid(logger, "match", word.ToString(), "contains, startsWith or exact"))
            : GenericResult<IListMatch>.Success(found);
    }

    // The identifiers ?id= names, repeated or comma separated, each once; one that is not a GUID, or more than MaxIds, is refused.
    private static IGenericResult<IReadOnlyList<Guid>> ReadIds(IQueryCollection query, ILogger logger)
    {
        var ids = new List<Guid>();
        if (!query.TryGetValue("id", out var named))
            return GenericResult<IReadOnlyList<Guid>>.Success(ids);

        // StringValues joins a repeated parameter with commas, so ?id=a&id=b and ?id=a,b read the same.
        foreach (var part in named.ToString().Split(','))
        {
            var identifier = part.Trim();
            if (!Guid.TryParse(identifier, out var id))
                return GenericResult<IReadOnlyList<Guid>>.Failure(EndpointLogger.PaginationValueInvalid(logger, "id", identifier, "a GUID"));

            if (!ids.Contains(id))
                ids.Add(id);

            if (ids.Count > MaxIds)
                return GenericResult<IReadOnlyList<Guid>>.Failure(EndpointLogger.PaginationValueInvalid(
                    logger, "id", $"more than {MaxIds} identifiers", $"at most {MaxIds} identifiers in one call"));
        }

        return GenericResult<IReadOnlyList<Guid>>.Success(ids);
    }

    /// <summary>Says whether any of the values a member offers matches the text, for a list that is searched in memory.</summary>
    /// <param name="values">The values to compare, such as a field's name and its description; a value that is absent is skipped.</param>
    /// <returns>True when there is no text to find, or any value matches it.</returns>
    /// <remarks>
    /// Compared without regard to case, in the way <see cref="Match"/> says. How the text sits in the value is read off the
    /// match's operator -- whether anything may precede it, whether anything may follow it -- so no match is named here.
    /// </remarks>
    public bool Selects(params string?[] values)
    {
        if (Text is not { } text) return true;

        foreach (var value in values)
        {
            if (value is not null && Compares(value, text)) return true;
        }

        return false;
    }

    private bool Compares(string value, string text)
    {
        var op = Match.Operator;
        if (!op.IsPatternMatch)
            return string.Equals(value, text, StringComparison.OrdinalIgnoreCase);

        if (op.MatchesAnythingBefore && op.MatchesAnythingAfter)
            return value.Contains(text, StringComparison.OrdinalIgnoreCase);

        return op.MatchesAnythingAfter
            ? value.StartsWith(text, StringComparison.OrdinalIgnoreCase)
            : value.EndsWith(text, StringComparison.OrdinalIgnoreCase);
    }
}
