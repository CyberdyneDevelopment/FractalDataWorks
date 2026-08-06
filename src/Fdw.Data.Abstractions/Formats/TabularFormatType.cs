using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Formats;

/// <summary>
/// Represents tabular/relational data format (SQL result sets, in-memory tables).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(FormatTypes), "Tabular", RestrictToCurrentCompilation = true)]
public sealed class TabularFormatType : FormatTypeBase
{

    /// <summary>
    /// Initializes a new instance of the <see cref="TabularFormatType"/> class.
    /// </summary>
    public TabularFormatType()
        : base(
            id: 1,
            name: "Tabular",
            displayName: "Tabular",
            description: "Tabular/relational data format for SQL result sets and in-memory tables",
            mimeType: "application/vnd.tabular",
            isBinary: false,
            supportsStreaming: true,
            // Why: Tabular is SQL result sets / in-memory tables, NOT a file format — empty extension
            // marks it non-file-addressable so a FileSystem-store builder fails loud on it (NO FALLBACKS).
            canonicalFileExtension: "")
    {
    }
}
