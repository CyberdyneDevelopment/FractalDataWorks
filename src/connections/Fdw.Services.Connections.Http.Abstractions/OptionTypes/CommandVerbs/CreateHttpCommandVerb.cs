using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.CommandVerbs;

/// <summary>Create is the insert by another name.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HttpCommandVerbs), "Create")]
public sealed class CreateHttpCommandVerb : HttpCommandVerbBase
{
    /// <summary>Initializes a new instance of the <see cref="CreateHttpCommandVerb"/> class.</summary>
    public CreateHttpCommandVerb()
        : base("Create", "POST", "create", "Create")
    {
    }
}
