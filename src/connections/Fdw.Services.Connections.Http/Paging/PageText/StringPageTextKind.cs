using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Paging.PageText;

/// <summary>A string where a text is named: the text is the string.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PageTextKinds), "String")]
public sealed class StringPageTextKind : PageTextKindBase
{
    /// <summary>Initializes a new instance of the <see cref="StringPageTextKind"/> class.</summary>
    public StringPageTextKind()
        : base("String", JsonValueKind.String)
    {
    }

    /// <inheritdoc/>
    public override string? Read(JsonElement value) => value.GetString();
}
