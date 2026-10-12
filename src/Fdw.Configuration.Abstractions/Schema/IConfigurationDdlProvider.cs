namespace Fdw.Configuration.Persistence.Schema;

/// <summary>
/// Interface for configuration classes that provide DDL definitions.
/// Implemented by generated code from [ManagedConfiguration] attribute.
/// </summary>
public interface IConfigurationDdlProvider
{
    /// <summary>
    /// Gets the DDL definition for this configuration type.
    /// </summary>
    /// <returns>The DDL definition containing table and column specifications.</returns>
    DdlDefinition GetDefinition();
}
