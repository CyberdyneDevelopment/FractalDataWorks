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
    /// Gets or sets the command timeout in seconds.
    /// </summary>
    public int CommandTimeout { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to enable connection pooling.
    /// </summary>
    public bool EnableConnectionPooling { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum pool size.
    /// </summary>
    public int MaxPoolSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether to use Azure AD authentication.
    /// </summary>
    public bool UseAzureAuthentication { get; set; }
}
