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
public partial class SecretManagerConfiguration : ISecretManagerConfiguration
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
    /// <param name="serviceOptionType">The service option type (e.g., "EnvironmentVariable", "AzureKeyVault").</param>
    protected SecretManagerConfiguration(string? serviceOptionType)
    {
        Implementation = serviceOptionType;
    }


    /// <summary>
    /// Gets or sets the durable logical identifier (matches sec.SecretManager.Id).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of this secret manager for lookup and display.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the domain this record belongs to.</summary>
    public string Domain => "SecretManager";

    /// <summary>Gets or sets the implementation this record names.</summary>
    [ValuesFrom(typeof(SecretManagerTypes))]
    public string? Implementation { get; set; }

    /// <summary>
    /// Gets the secret manager type name. Alias for <see cref="Implementation"/>.
    /// </summary>
    public string? SecretManagerType => Implementation;

    /// <summary>
    /// Gets or sets the optional description of this secret manager.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the deployment environment this secret manager targets (e.g., Local, Dev, QA, Prod).
    /// </summary>
    [ValuesFrom(typeof(EnvironmentTypes))]
    public string? Environment { get; set; }

    /// <summary>
    /// Gets or sets the typed secret manager body for this header row.
    /// Populated on the read path after loading the typed body table row.
    /// Not persisted — the typed body is saved separately to its own table.
    /// </summary>
    [NotMapped]
    public ISecretManagerImplementationConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    /// <remarks>
    /// The non-generic view of <see cref="Configuration"/>. The platform service provider reads the
    /// implementation without naming this domain's implementation contract; netstandard2.0 rules out
    /// a default interface implementation, so each domain record states it.
    /// </remarks>
    IGenericConfiguration? IDomainConfiguration.ImplementationConfiguration => Configuration;

}
