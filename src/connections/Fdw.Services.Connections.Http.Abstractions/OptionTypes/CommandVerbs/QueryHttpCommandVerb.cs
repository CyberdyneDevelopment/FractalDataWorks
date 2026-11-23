using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.CommandVerbs;

/// <summary>A query reads: GET, a GraphQL query, an operation named Get{Type}.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HttpCommandVerbs), "Query")]
public sealed class QueryHttpCommandVerb : HttpCommandVerbBase
{
    /// <summary>Initializes a new instance of the <see cref="QueryHttpCommandVerb"/> class.</summary>
    public QueryHttpCommandVerb()
        : base("Query", "GET", null, "Get")
    {
    }
}
