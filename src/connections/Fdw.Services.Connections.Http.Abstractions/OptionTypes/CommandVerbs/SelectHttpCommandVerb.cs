using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.CommandVerbs;

/// <summary>Select is sent as a query operation named Get{Type}; the HTTP method is POST.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HttpCommandVerbs), "Select")]
public sealed class SelectHttpCommandVerb : HttpCommandVerbBase
{
    /// <summary>Initializes a new instance of the <see cref="SelectHttpCommandVerb"/> class.</summary>
    public SelectHttpCommandVerb()
        : base("Select", "POST", null, "Get")
    {
    }
}
