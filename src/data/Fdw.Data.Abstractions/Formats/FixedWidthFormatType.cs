using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Formats;

/// <summary>
/// Represents the fixed-width text data format: each field occupies a declared column range of the line.
/// </summary>
/// <remarks>
/// Without this a data store of the FileSystem type cannot address a fixed-width file: a container's format resolves through
/// <see cref="FormatTypes"/>, and the record source and writer of this name already exist in <c>RecordSourceTypes</c> and
/// <c>RecordWriterTypes</c>. The format has no standard file extension; <c>.txt</c> is the plain-text one.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(FormatTypes), "FixedWidth", RestrictToCurrentCompilation = true)]
public sealed class FixedWidthFormatType : FormatTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FixedWidthFormatType"/> class.
    /// </summary>
    public FixedWidthFormatType()
        : base(
            name: "FixedWidth",
            displayName: "Fixed-width text",
            description: "Fixed-width text data format; each field has a declared column range",
            mimeType: "text/plain",
            isBinary: false,
            supportsStreaming: true,
            canonicalFileExtension: ".txt",
            family: "FixedWidth")
    {
    }
}
