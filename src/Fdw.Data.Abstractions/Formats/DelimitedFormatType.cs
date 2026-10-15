using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Formats;

/// <summary>
/// Represents the delimited text data format (CSV and its relatives: a character separates the fields).
/// </summary>
/// <remarks>
/// The plain format of the <c>Delimited</c> family, for a file whose separator is neither of the named formats
/// (<c>Csv</c>, <c>Tsv</c>): the container states the separator. It is the only format of the family that accepts one; the
/// named formats define theirs. The family name is also the name of the record source and writer that read and write them.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(FormatTypes), "Delimited", RestrictToCurrentCompilation = true)]
public sealed class DelimitedFormatType : FormatTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedFormatType"/> class.
    /// </summary>
    public DelimitedFormatType()
        : base(
            name: "Delimited",
            displayName: "Delimited text",
            description: "Delimited text data format with a separator the container states; Csv and Tsv are its named formats",
            mimeType: "text/csv",
            isBinary: false,
            supportsStreaming: true,
            canonicalFileExtension: ".csv",
            family: "Delimited")
    {
    }
}
