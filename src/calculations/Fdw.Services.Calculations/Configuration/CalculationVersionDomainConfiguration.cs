using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Calculations.Configuration;

/// <summary>
/// The domain row of a version of a calculation held at an organization or a workspace (FDW-2560): which calculation it varies, where it is held, and
/// the words it gives the calculation there. The calculation itself is the tenant's version and has no row here.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class CalculationVersionDomainConfiguration : DomainConfiguration
{
    /// <summary>Gets or sets the id of the calculation this is a version of.</summary>
    public Guid BaseCalculationId { get; set; }

    /// <summary>Gets or sets the name of the calculation this is a version of.</summary>
    public string CalculationName { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind of place the version is held at: Organization or Workspace.</summary>
    public string ScopeKind { get; set; } = string.Empty;

    /// <summary>Gets or sets the id of the organization or workspace the version is held at.</summary>
    public Guid ScopeId { get; set; }

    /// <summary>Gets or sets what the calculation is, in this place's words.</summary>
    public string Description { get; set; } = string.Empty;
}
