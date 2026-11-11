using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Formats;

/// <summary>
/// Represents JSON Lines: one JSON record per line (FDW-4971).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(FormatTypes), "JsonLines", RestrictToCurrentCompilation = true)]
public sealed class JsonLinesFormatType : FormatTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonLinesFormatType"/> class.
    /// </summary>
    public JsonLinesFormatType()
        : base(
            name: "JsonLines",
            displayName: "JSON Lines",
            description: "JSON Lines: one JavaScript Object Notation record per line",
            mimeType: "application/jsonl",
            isBinary: false,
            supportsStreaming: true,
            canonicalFileExtension: ".jsonl",
            family: "JsonLines")
    {
    }
}
