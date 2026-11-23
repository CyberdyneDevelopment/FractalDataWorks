using System.Text.Json;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Paging.PageText;

/// <summary>A kind of JSON value a page's body may hold at a path that names a text (a next link, a continuation token), and how it reads as text.</summary>
public interface IPageTextKind : ITypeOption<int, IPageTextKind>
{
    /// <summary>Gets the kind of JSON value this option reads.</summary>
    [TypeLookup("ByValueKind")]
    JsonValueKind Kind { get; }

    /// <summary>Reads the value as text.</summary>
    /// <param name="value">The value, of this option's <see cref="Kind"/>.</param>
    /// <returns>The text; null when the value is a null, there being no more pages.</returns>
    string? Read(JsonElement value);
}
