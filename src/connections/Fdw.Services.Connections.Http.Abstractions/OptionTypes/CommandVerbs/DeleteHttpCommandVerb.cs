using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.CommandVerbs;

/// <summary>A delete removes: DELETE, the GraphQL delete mutation, Delete{Type}.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HttpCommandVerbs), "Delete")]
public sealed class DeleteHttpCommandVerb : HttpCommandVerbBase
{
    /// <summary>Initializes a new instance of the <see cref="DeleteHttpCommandVerb"/> class.</summary>
    public DeleteHttpCommandVerb()
        : base("Delete", "DELETE", "delete", "Delete")
    {
    }
}
