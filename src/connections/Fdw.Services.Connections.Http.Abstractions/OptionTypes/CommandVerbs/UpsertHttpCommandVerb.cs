using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.CommandVerbs;

/// <summary>The Upsert command is sent with POST as a GraphQL query and names no operation.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HttpCommandVerbs), "Upsert")]
public sealed class UpsertHttpCommandVerb : HttpCommandVerbBase
{
    /// <summary>Initializes a new instance of the <see cref="UpsertHttpCommandVerb"/> class.</summary>
    public UpsertHttpCommandVerb()
        : base("Upsert", "POST", null, null)
    {
    }
}
