using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.CommandVerbs;

/// <summary>The Read command is sent with POST as a GraphQL query and names no operation.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HttpCommandVerbs), "Read")]
public sealed class ReadHttpCommandVerb : HttpCommandVerbBase
{
    /// <summary>Initializes a new instance of the <see cref="ReadHttpCommandVerb"/> class.</summary>
    public ReadHttpCommandVerb()
        : base("Read", "POST", null, null)
    {
    }
}
