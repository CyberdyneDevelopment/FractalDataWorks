using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Calculations.Configuration;

/// <summary>
/// The calculation entities domain's record: the four properties every domain has, and everything a list of
/// calculation entities shows.
/// </summary>
/// <remarks>
/// Maps to <c>calc.CalculationEntity</c> in ConfigurationDb. Everything a calculation is beyond these fields
/// lives on its per-kind implementation (Formula, Windowed); these live here, on the domain row, so a page of
/// calculations is one read of one table. Reading a calculation stamps them onto its implementation; saving one
/// whose values differ writes a new version of this row from them (FDW-978, FDW-1006). The defaults are the
/// implementation's own.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class CalculationEntityDomainConfiguration : DomainConfiguration
{
    /// <summary>Gets or sets the calculation's description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether the calculation is enabled.</summary>
    public bool IsEnabled { get; set; } = true;
}
