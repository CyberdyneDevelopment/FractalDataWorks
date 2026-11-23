using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>The JSON word false.</summary>
[TypeOption(typeof(JsonInputKinds), "False", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FalseJsonInputKind : JsonInputKindBase
{
    /// <summary>Initializes a new instance of the <see cref="FalseJsonInputKind"/> class.</summary>
    public FalseJsonInputKind()
        : base("False", JsonValueKind.False)
    {
    }

    /// <inheritdoc />
    public override object? Read(JsonElement value) => false;
}
