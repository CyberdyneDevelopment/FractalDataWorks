using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fdw.Services.Connections;

/// <summary>
/// Base configuration class for all data store types.
/// Generates the parent table <c>data.DataStore</c> which contains core fields shared by all store types.
/// </summary>
/// <remarks>
/// <para>
/// A DataStore represents a physical storage location accessible via a Connection.
/// The same physical database may have multiple DataStores when accessed via different
/// Connections with different credentials/identities.
/// </para>
/// <para>
/// Hierarchy:
/// <list type="bullet">
/// <item><description><c>DataStore</c> (this) - Root configuration, references Connection</description></item>
/// <item><description><c>DataPath</c> - Navigation within store</description></item>
/// <item><description><c>DataContainer</c> - Physical schema at a path</description></item>
/// <item><description><c>DataContainerField</c> - Column/property definition</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "DataStore")]
public partial class DataStoreConfiguration : DomainConfigurationBase, IGenericConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreConfiguration"/> class.
    /// Default constructor for IOptions binding and header lookups.
    /// </summary>
    public DataStoreConfiguration()
    {
    }















}
