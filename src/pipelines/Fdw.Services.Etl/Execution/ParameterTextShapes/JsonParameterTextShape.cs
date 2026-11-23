using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Execution;

/// <summary>Every other value, written as JSON; no value matches it, it is what <c>Of</c> answers when no other shape does.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ParameterTextShapes), "Json")]
public sealed class JsonParameterTextShape : ParameterTextShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="JsonParameterTextShape"/> class.</summary>
    public JsonParameterTextShape()
        : base("Json")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => false;

    /// <inheritdoc />
    public override string Write(object value) => JsonSerializer.Serialize(value);
}
