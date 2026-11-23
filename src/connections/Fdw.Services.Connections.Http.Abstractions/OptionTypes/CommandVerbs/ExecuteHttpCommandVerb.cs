using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.CommandVerbs;

/// <summary>The Execute command is sent with POST as a GraphQL query and names no operation.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HttpCommandVerbs), "Execute")]
public sealed class ExecuteHttpCommandVerb : HttpCommandVerbBase
{
    /// <summary>Initializes a new instance of the <see cref="ExecuteHttpCommandVerb"/> class.</summary>
    public ExecuteHttpCommandVerb()
        : base("Execute", "POST", null, null)
    {
    }
}
