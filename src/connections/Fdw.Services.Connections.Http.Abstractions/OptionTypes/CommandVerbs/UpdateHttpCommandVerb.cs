using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.CommandVerbs;

/// <summary>An update changes: PUT, the GraphQL update mutation, Update{Type}.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HttpCommandVerbs), "Update")]
public sealed class UpdateHttpCommandVerb : HttpCommandVerbBase
{
    /// <summary>Initializes a new instance of the <see cref="UpdateHttpCommandVerb"/> class.</summary>
    public UpdateHttpCommandVerb()
        : base("Update", "PUT", "update", "Update")
    {
    }
}
