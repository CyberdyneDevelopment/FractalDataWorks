using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Formats;

/// <summary>
/// Represents JSON data format.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(FormatTypes), "Json", RestrictToCurrentCompilation = true)]
public sealed class JsonFormatType : FormatTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonFormatType"/> class.
    /// </summary>
    public JsonFormatType()
        : base(
            id: 2,
            name: "Json",
            displayName: "JSON",
            description: "JavaScript Object Notation data format",
            mimeType: "application/json",
            isBinary: false,
            supportsStreaming: true,
            canonicalFileExtension: ".json")
    {
    }
}
