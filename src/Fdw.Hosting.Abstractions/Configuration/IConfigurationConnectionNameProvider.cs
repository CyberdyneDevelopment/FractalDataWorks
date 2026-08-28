namespace Fdw.Hosting.Abstractions.Configuration;

/// <summary>
/// Provides the connection name used by endpoint base classes to query configuration data.
/// Replaces hardcoded "PlatformConfiguration" defaults across all endpoint bases.
/// </summary>
public interface IConfigurationConnectionNameProvider
{
    /// <summary>Gets the connection name for configuration database queries.</summary>
    string ConnectionName { get; }
}
