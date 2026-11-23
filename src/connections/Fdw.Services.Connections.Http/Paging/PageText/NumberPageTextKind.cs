using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Paging.PageText;

/// <summary>A number where a text is named (a page number as a continuation token): the text is the number as written.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PageTextKinds), "Number")]
public sealed class NumberPageTextKind : PageTextKindBase
{
    /// <summary>Initializes a new instance of the <see cref="NumberPageTextKind"/> class.</summary>
    public NumberPageTextKind()
        : base("Number", JsonValueKind.Number)
    {
    }

    /// <inheritdoc/>
    public override string? Read(JsonElement value) => value.GetRawText();
}
