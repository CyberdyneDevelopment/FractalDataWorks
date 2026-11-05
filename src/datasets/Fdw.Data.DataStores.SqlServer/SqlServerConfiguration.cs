using Fdw.Configuration;

namespace Fdw.Data.DataStores.SqlServer;

/// <summary>
/// Configuration for SQL Server data store.
/// </summary>
public sealed class SqlServerConfiguration
{
    /// <summary>
    /// Gets or sets the connection string for the SQL Server instance.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to use Azure AD authentication.
    /// </summary>
    public bool UseAzureAuthentication { get; set; }
}
