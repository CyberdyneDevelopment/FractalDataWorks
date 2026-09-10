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
public partial class AegisCommandConfiguration : DomainConfigurationBase, IDomainConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AegisCommandConfiguration"/> class.
    /// Default constructor for IOptions binding and header lookups.
    /// </summary>
    public AegisCommandConfiguration()
    {
    }





}
