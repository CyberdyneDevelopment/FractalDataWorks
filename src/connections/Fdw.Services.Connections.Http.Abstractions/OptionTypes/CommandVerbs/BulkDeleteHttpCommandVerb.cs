using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.CommandVerbs;

/// <summary>The BulkDelete command is sent with POST as a GraphQL query and names no operation.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HttpCommandVerbs), "BulkDelete")]
public sealed class BulkDeleteHttpCommandVerb : HttpCommandVerbBase
{
    /// <summary>Initializes a new instance of the <see cref="BulkDeleteHttpCommandVerb"/> class.</summary>
    public BulkDeleteHttpCommandVerb()
        : base("BulkDelete", "POST", null, null)
    {
    }
}
