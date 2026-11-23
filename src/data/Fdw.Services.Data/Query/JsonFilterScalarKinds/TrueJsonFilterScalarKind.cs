using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>The JSON word true.</summary>
[TypeOption(typeof(JsonFilterScalarKinds), "True")]
[ExcludeFromCodeCoverage]
public sealed class TrueJsonFilterScalarKind : JsonFilterScalarKindBase
{
    /// <summary>Initializes a new instance of the <see cref="TrueJsonFilterScalarKind"/> class.</summary>
    public TrueJsonFilterScalarKind()
        : base("True", JsonValueKind.True)
    {
    }

    /// <inheritdoc />
    public override object? Read(JsonElement value) => true;
}
