using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Aegis.Abstractions;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Aegis.Configuration;

/// <summary>
/// Parent-header configuration for a declared Aegis command: identity + which connection it
/// targets + the approval-policy discriminator. Mirrors <c>ConnectionConfiguration</c>.
/// </summary>
/// <remarks>
/// <para>
/// This class serves two purposes, exactly like <c>ConnectionConfiguration</c>:
/// <list type="bullet">
/// <item><description>As a header configuration for <c>ConfigurationSchema.Commands</c> / IOptions lookups</description></item>
/// <item><description>As the base identity row a typed body (<c>PreApprovedCommandConfiguration</c>,
/// <c>AdHocCommandConfiguration</c>) links back to via <c>AegisCommandId</c>.</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "AegisCommand")]
public partial class AegisCommandConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AegisCommandConfiguration"/> class.
    /// Default constructor for IOptions binding and header lookups.
    /// </summary>
    public AegisCommandConfiguration() : this("AegisCommand", null, "Commands")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AegisCommandConfiguration"/> class.
    /// Protected constructor for derived classes to set their type identity.
    /// </summary>
    /// <param name="serviceType">The service type (domain) - always "AegisCommand".</param>
    /// <param name="serviceOptionType">The approval-policy kind (e.g., "PreApproved", "AdHoc").</param>
    /// <param name="sectionName">The configuration section name for binding.</param>
    protected AegisCommandConfiguration(string serviceType, string? serviceOptionType, string sectionName)
    {
        ServiceType = serviceType;
        ServiceOptionType = serviceOptionType;
        SectionName = sectionName;
    }

    /// <summary>
    /// Gets or sets the durable logical identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of this command for lookup and display.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the declared connection this command targets.
    /// </summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the section name for configuration binding.
    /// </summary>
    public string SectionName { get; set; }

    /// <summary>
    /// Gets or sets the service type (domain) - always "AegisCommand" for this configuration.
    /// </summary>
    public string ServiceType { get; set; }

    /// <summary>
    /// Gets or sets the approval-policy kind discriminator (e.g., "PreApproved", "AdHoc").
    /// </summary>
    [ValuesFrom(typeof(ApprovalPolicyTypes))]
    public string? ServiceOptionType { get; set; }

    /// <summary>
    /// Gets or sets the typed approval-policy body for this command header row. Populated on the
    /// read path by the provider after loading the typed body table row. Not persisted — the typed
    /// body is saved separately to its own table.
    /// </summary>
    /// <remarks>
    /// Why: [NotMapped] — this property is not a column on the AegisCommand header row. The read
    /// path populates this by dispatching on ServiceOptionType to the appropriate typed provider,
    /// mirroring <c>ConnectionConfiguration.Configuration</c>.
    /// </remarks>
    [NotMapped]
    public IApprovalPolicyConfiguration? Configuration { get; set; }
}
