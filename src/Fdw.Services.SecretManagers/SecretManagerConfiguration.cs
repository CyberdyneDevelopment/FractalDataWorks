using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.SecretManagers.Abstractions;

namespace Fdw.Services.SecretManagers;

/// <summary>
/// Base configuration class for all secret manager types.
/// Generates the parent table <c>sec.SecretManager</c> which contains core identity fields shared by all secret manager types.
/// </summary>
/// <remarks>
/// <para>
/// This class serves two purposes:
/// <list type="bullet">
/// <item><description>As a header configuration for <c>IOptionsSnapshot&lt;List&lt;SecretManagerConfiguration&gt;&gt;</c> lookups</description></item>
/// <item><description>As the base class for type-specific configurations (EnvironmentVariableConfiguration, etc.)</description></item>
/// </list>
/// </para>
/// <para>
/// The implementation this record names is read from the row, never compiled into a type.
/// Derived classes call the protected constructor to set their specific values.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "SecretManager")]
public partial class SecretManagerConfiguration : DomainConfigurationBase, ISecretManagerConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecretManagerConfiguration"/> class.
    /// Default constructor for IOptions binding and header lookups.
    /// </summary>
    public SecretManagerConfiguration() : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretManagerConfiguration"/> class.
    /// Protected constructor for derived classes to set their type identity.
    /// </summary>
    /// <param name="implementation">The service option type (e.g., "EnvironmentVariable", "AzureKeyVault").</param>
    protected SecretManagerConfiguration(string? implementation)
    {
        Implementation = implementation;
    }









}
