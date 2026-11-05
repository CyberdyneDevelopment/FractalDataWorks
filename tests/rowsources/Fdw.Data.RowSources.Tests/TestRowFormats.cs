using System.Collections.Generic;

namespace Fdw.Data.RowSources.Tests;

/// <summary>
/// The format choices the tests state when they build writer options from a column list (FDW-3823): a writer takes no choice of its own,
/// so a test states every one a format reads, with the values the writers used before choices had to be stated.
/// </summary>
internal static class TestRowFormats
{
    /// <summary>Gets the stated choices every writer format reads.</summary>
    public static IReadOnlyDictionary<string, object> Choices { get; } = new Dictionary<string, object>(System.StringComparer.Ordinal)
    {
        ["HasHeader"] = true,
        ["Separator"] = ",",
        ["QuoteChar"] = "\"",
        ["WriteByteOrderMark"] = false,
        ["NeutralizeFormulas"] = false,
        ["SkipValidation"] = false,
        ["RootElementName"] = "Rows",
        ["RowElementName"] = "Row",
        ["OmitXmlDeclaration"] = true,
        ["SheetName"] = "Data",
    };
}
