using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Paging.PageText;

/// <summary>A null where a text is named: the remote has no more pages, so there is no text.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PageTextKinds), "Null")]
public sealed class NullPageTextKind : PageTextKindBase
{
    /// <summary>Initializes a new instance of the <see cref="NullPageTextKind"/> class.</summary>
    public NullPageTextKind()
        : base("Null", JsonValueKind.Null)
    {
    }

    /// <inheritdoc/>
    public override string? Read(JsonElement value) => null;
}
