using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.CommandVerbs;

/// <summary>An insert creates: POST, the GraphQL create mutation, Create{Type}.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HttpCommandVerbs), "Insert")]
public sealed class InsertHttpCommandVerb : HttpCommandVerbBase
{
    /// <summary>Initializes a new instance of the <see cref="InsertHttpCommandVerb"/> class.</summary>
    public InsertHttpCommandVerb()
        : base("Insert", "POST", "create", "Create")
    {
    }
}
