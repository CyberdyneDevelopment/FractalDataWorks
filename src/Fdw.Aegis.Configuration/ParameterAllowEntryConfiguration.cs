using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Aegis.Configuration;

/// <summary>
/// A single deterministic-input declaration: one parameter a pre-approved command accepts, the
/// values it permits, and whether it must be present.
/// </summary>
/// <remarks>
/// A child of <see cref="PreApprovedCommandConfiguration"/>: its rows hang from the implementation
/// row by <see cref="PreApprovedCommandId"/>, and load and save with it.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class ParameterAllowEntryConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the unique identifier for this entry.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the logical FK to the owning <see cref="PreApprovedCommandConfiguration"/>.</summary>
    public Guid PreApprovedCommandId { get; set; }

    /// <summary>Gets or sets the parameter name this entry declares.</summary>
    public string ParameterName { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this parameter must be present on every request.</summary>
    public bool Required { get; set; }

    /// <summary>Gets or sets the values permitted for this parameter.</summary>
    public IList<PermittedValueConfiguration> PermittedValues { get; set; } = new List<PermittedValueConfiguration>();
}
