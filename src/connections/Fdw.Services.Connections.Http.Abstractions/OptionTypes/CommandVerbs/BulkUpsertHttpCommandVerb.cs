using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.CommandVerbs;

/// <summary>The BulkUpsert command is sent with POST as a GraphQL query and names no operation.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HttpCommandVerbs), "BulkUpsert")]
public sealed class BulkUpsertHttpCommandVerb : HttpCommandVerbBase
{
    /// <summary>Initializes a new instance of the <see cref="BulkUpsertHttpCommandVerb"/> class.</summary>
    public BulkUpsertHttpCommandVerb()
        : base("BulkUpsert", "POST", null, null)
    {
    }
}
