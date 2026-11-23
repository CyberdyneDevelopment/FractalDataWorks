using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// The DataSet domain's record: the four properties every domain has, and the two other names a person reads a data set by (FDW-6129).
/// </summary>
/// <remarks>
/// Maps to <c>data.DataSet</c>. A list of data sets filters on a data set's name, display name and abbreviation and shows them, so they live
/// here, on the domain row, beside <see cref="DomainConfiguration.Name"/>: a page, a search of their text and a count are one read of one
/// table, and a screen chooses which of the three it shows. Reading a data set stamps them onto its implementation; saving one whose values
/// differ writes a new version of this row from them, as for the other domains that list fields. The data set type tables' own columns of the same names
/// stay until the release that removes them; nothing reads them.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class DataSetDomainConfiguration : DomainConfiguration
{
    /// <summary>Gets or sets the name a person reads the data set by; null when it has none.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the short name a person may read the data set by, at most 20 characters; null when it has none.</summary>
    public string? Abbreviation { get; set; }
}
