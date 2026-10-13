using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Formats;

/// <summary>
/// Represents Comma-separated values text data format.
/// </summary>
/// <remarks>
/// A format of the <c>Delimited</c> family: it is read and written by the <c>Delimited</c> record source and writer and
/// defines its own separator (a comma) and extension (<c>.csv</c>). A container of this format states no separator;
/// the plain <c>Delimited</c> format is for any other separator.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(FormatTypes), "Csv", RestrictToCurrentCompilation = true)]
public sealed class CsvFormatType : FormatTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CsvFormatType"/> class.
    /// </summary>
    public CsvFormatType()
        : base(
            name: "Csv",
            displayName: "CSV",
            description: "Comma-separated values text data format.",
            mimeType: "text/csv",
            isBinary: false,
            supportsStreaming: true,
            canonicalFileExtension: ".csv",
            family: "Delimited",
            separator: ",")
    {
    }
}
