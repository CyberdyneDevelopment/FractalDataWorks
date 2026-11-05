using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Operations.Configuration;

/// <summary>
/// A threshold (FDW-3936): an ordered list of rules over the values one kind of subject reports. The Implementation of its domain row names the threshold type (an option
/// of ThresholdTypes), which says what subject it judges and which values a rule may name. It is attached to subjects through the link table of each subject kind.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Operations")]
public sealed partial class ThresholdImplementationConfiguration : IThresholdImplementationConfiguration
{
    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted. It is the name of the threshold type.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name, unique in a tenant.</summary>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? Description { get; set; }

    /// <inheritdoc/>
    public bool IsEnabled { get; set; }

    /// <inheritdoc/>
    public bool FiresOnEntry { get; set; }

    /// <inheritdoc/>
    public string SubjectKind { get; set; } = string.Empty;

    /// <inheritdoc/>
    public IList<ThresholdParameterConfiguration> Parameters { get; set; } = [];

    /// <inheritdoc/>
    public IList<ThresholdRuleConfiguration> Rules { get; set; } = [];
}
