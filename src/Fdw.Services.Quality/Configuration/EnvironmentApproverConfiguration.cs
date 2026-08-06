using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Quality.Configuration;

/// <summary>
/// Configuration for an individual environment approver.
/// Child of EnvironmentConfiguration.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Promotion")]
public sealed partial class EnvironmentApproverConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the name for display/binding.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the approver name.
    /// </summary>
    public string ApproverName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the approval order (1-based).
    /// </summary>
    public int ApprovalOrder { get; set; }
}
