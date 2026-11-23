using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.CommandVerbs;

/// <summary>The Stream command is sent with POST as a GraphQL query and names no operation.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HttpCommandVerbs), "Stream")]
public sealed class StreamHttpCommandVerb : HttpCommandVerbBase
{
    /// <summary>Initializes a new instance of the <see cref="StreamHttpCommandVerb"/> class.</summary>
    public StreamHttpCommandVerb()
        : base("Stream", "POST", null, null)
    {
    }
}
