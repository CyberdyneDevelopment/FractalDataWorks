using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Formats;

/// <summary>
/// Represents Tab-separated values text data format.
/// </summary>
/// <remarks>
/// A format of the <c>Delimited</c> family: it is read and written by the <c>Delimited</c> record source and writer and
/// defines its own separator (a tab) and extension (<c>.tsv</c>). A container of this format states no separator;
/// the plain <c>Delimited</c> format is for any other separator.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(FormatTypes), "Tsv", RestrictToCurrentCompilation = true)]
public sealed class TsvFormatType : FormatTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TsvFormatType"/> class.
    /// </summary>
    public TsvFormatType()
        : base(
            name: "Tsv",
            displayName: "TSV",
            description: "Tab-separated values text data format.",
            mimeType: "text/tab-separated-values",
            isBinary: false,
            supportsStreaming: true,
            canonicalFileExtension: ".tsv",
            family: "Delimited",
            separator: "\t")
    {
    }
}
