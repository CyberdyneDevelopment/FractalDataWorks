using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Credentials.Abstractions;

namespace Fdw.Services.Credentials;

/// <summary>
/// Header configuration class for all credential service types.
/// Generates the parent table <c>sec.CredentialService</c> which contains core identity fields
/// shared by all credential service types.
/// </summary>
/// <remarks>
/// <para>
/// Serves as the header row for the polymorphic credential service configuration pattern:
/// <list type="bullet">
/// <item><description>As a header for <c>IOptionsMonitor&lt;List&lt;CredentialServiceConfiguration&gt;&gt;</c> lookups</description></item>
/// <item><description>The typed body row is loaded separately by <see cref="CredentialServiceConfigurationProvider"/>
/// and attached to <see cref="Configuration"/> via discriminator dispatch on <see cref="Implementation"/>.</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "CredentialService")]
public partial class CredentialServiceConfiguration : DomainConfigurationBase, ICredentialServiceConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialServiceConfiguration"/> class.
    /// Default constructor for IOptions binding and header lookups.
    /// </summary>
    public CredentialServiceConfiguration() : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialServiceConfiguration"/> class.
    /// Protected constructor for derived classes to set their type identity.
    /// </summary>
    /// <param name="implementation">The service option type (e.g., "Sql").</param>
    protected CredentialServiceConfiguration(string? implementation)
    {
        Implementation = implementation;
    }










}
