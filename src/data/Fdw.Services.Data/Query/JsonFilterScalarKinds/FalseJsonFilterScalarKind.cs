using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>The JSON word false.</summary>
[TypeOption(typeof(JsonFilterScalarKinds), "False")]
[ExcludeFromCodeCoverage]
public sealed class FalseJsonFilterScalarKind : JsonFilterScalarKindBase
{
    /// <summary>Initializes a new instance of the <see cref="FalseJsonFilterScalarKind"/> class.</summary>
    public FalseJsonFilterScalarKind()
        : base("False", JsonValueKind.False)
    {
    }

    /// <inheritdoc />
    public override object? Read(JsonElement value) => false;
}
