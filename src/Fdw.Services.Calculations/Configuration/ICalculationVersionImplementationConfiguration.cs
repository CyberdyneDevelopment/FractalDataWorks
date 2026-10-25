using System;
using Fdw.Configuration;

namespace Fdw.Services.Calculations.Configuration;

/// <summary>A version of a calculation held at an organization or a workspace (FDW-2560).</summary>
public interface ICalculationVersionImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the id of the calculation this is a version of.</summary>
    Guid BaseCalculationId { get; set; }

    /// <summary>Gets or sets the name of the calculation this is a version of.</summary>
    string CalculationName { get; set; }

    /// <summary>Gets or sets the kind of place the version is held at: Organization or Workspace.</summary>
    string ScopeKind { get; set; }

    /// <summary>Gets or sets the id of the organization or workspace the version is held at.</summary>
    Guid ScopeId { get; set; }

    /// <summary>Gets or sets what the calculation is, in this place's words.</summary>
    string Description { get; set; }
}
