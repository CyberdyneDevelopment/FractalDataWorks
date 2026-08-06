using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Formats;

/// <summary>
/// Represents CSV (Comma-Separated Values) data format.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(FormatTypes), "Csv", RestrictToCurrentCompilation = true)]
public sealed class CsvFormatType : FormatTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CsvFormatType"/> class.
    /// </summary>
    public CsvFormatType()
        : base(
            id: 4,
            name: "Csv",
            displayName: "CSV",
            description: "Comma-Separated Values data format",
            mimeType: "text/csv",
            isBinary: false,
            supportsStreaming: true,
            canonicalFileExtension: ".csv")
    {
    }
}
