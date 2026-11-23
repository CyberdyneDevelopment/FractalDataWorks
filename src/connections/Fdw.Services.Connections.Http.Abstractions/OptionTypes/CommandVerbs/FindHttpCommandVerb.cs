using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.CommandVerbs;

/// <summary>The Find command is sent with POST as a GraphQL query and names no operation.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HttpCommandVerbs), "Find")]
public sealed class FindHttpCommandVerb : HttpCommandVerbBase
{
    /// <summary>Initializes a new instance of the <see cref="FindHttpCommandVerb"/> class.</summary>
    public FindHttpCommandVerb()
        : base("Find", "POST", null, null)
    {
    }
}
