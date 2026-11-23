using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Paging.PageText;

/// <summary>The type the page text kind collection is built on.</summary>
[ExcludeFromCodeCoverage]
public abstract class PageTextKindBase : TypeOptionBase<PageTextKindBase>, IPageTextKind
{
    /// <summary>Initializes the option with its name and the JSON kind the collection finds it by.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="kind">The kind of JSON value it reads.</param>
    protected PageTextKindBase(string name, JsonValueKind kind)
        : base(name)
    {
        Kind = kind;
    }

    /// <inheritdoc/>
    [TypeLookup("ByValueKind")]
    public JsonValueKind Kind { get; }

    /// <inheritdoc/>
    public abstract string? Read(JsonElement value);
}
