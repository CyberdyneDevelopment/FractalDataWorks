using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Formats;

/// <summary>
/// Represents the Excel workbook (.xlsx) data format.
/// </summary>
/// <remarks>
/// Without this a data store of the FileSystem type cannot address a workbook: a container's format resolves
/// through <see cref="FormatTypes"/>, and the file's extension comes from <see cref="FormatTypeBase.CanonicalFileExtension"/>.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(FormatTypes), "Xlsx", RestrictToCurrentCompilation = true)]
public sealed class XlsxFormatType : FormatTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="XlsxFormatType"/> class.
    /// </summary>
    public XlsxFormatType()
        : base(
            name: "Xlsx",
            displayName: "Excel workbook",
            description: "Microsoft Excel workbook (.xlsx), a zip of XML parts",
            mimeType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            isBinary: true,
            supportsStreaming: false,
            canonicalFileExtension: ".xlsx",
            family: "Xlsx")
    {
    }
}
