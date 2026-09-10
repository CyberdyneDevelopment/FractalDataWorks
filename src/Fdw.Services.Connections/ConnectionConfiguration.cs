using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections;

/// <summary>
/// Base configuration class for all connection types.
/// Generates the parent table <c>conn.Connection</c> which contains core identity fields shared by all connection types.
/// </summary>
/// <remarks>
/// <para>
/// This class serves two purposes:
/// <list type="bullet">
/// <item><description>As a header configuration for <c>IOptionsSnapshot&lt;List&lt;ConnectionConfiguration&gt;&gt;</c> lookups</description></item>
/// <item><description>As the base class for type-specific configurations (MsSqlConnectionConfiguration, etc.)</description></item>
/// </list>
/// </para>
/// <para>
/// The implementation this record names is read from the row, never compiled into a type.
/// Derived classes call the protected constructor to set their specific values.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Connection")]
public partial class ConnectionConfiguration : DomainConfigurationBase, IConnectionConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionConfiguration"/> class.
    /// Default constructor for IOptions binding and header lookups.
    /// </summary>
    public ConnectionConfiguration() : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionConfiguration"/> class.
    /// Protected constructor for derived classes to set their type identity.
    /// </summary>
    /// <param name="implementation">The implementation this record names (e.g., "MsSql", "Rest", "Http").</param>
    protected ConnectionConfiguration(string? implementation)
    {
        Implementation = implementation;
    }















}
