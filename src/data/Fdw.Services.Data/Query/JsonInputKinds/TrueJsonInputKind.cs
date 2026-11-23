using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>The JSON word true.</summary>
[TypeOption(typeof(JsonInputKinds), "True", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TrueJsonInputKind : JsonInputKindBase
{
    /// <summary>Initializes a new instance of the <see cref="TrueJsonInputKind"/> class.</summary>
    public TrueJsonInputKind()
        : base("True", JsonValueKind.True)
    {
    }

    /// <inheritdoc />
    public override object? Read(JsonElement value) => true;
}
