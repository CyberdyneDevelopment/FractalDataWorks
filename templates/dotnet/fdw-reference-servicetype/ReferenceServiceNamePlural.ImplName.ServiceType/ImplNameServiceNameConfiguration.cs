namespace ReferenceServiceNamePlural.ImplName;

/// <summary>
/// Configuration for the ImplName ServiceName.
/// </summary>
// Why: in FDW proper the configuration POCO stays in the FRAMEWORK package, not the reference
// implementation — it is the contract the framework reads and writes, and the DDL is generated from
// its ConfigurationCommand. It is here only so a freshly generated pair compiles standalone. When you
// wire this to a real domain, delete this file and close the base on the domain's own configuration
// type, adding [ManagedConfiguration] there rather than here.
public sealed class ImplNameServiceNameConfiguration
{
    /// <summary>Gets or sets the configured name of this service instance.</summary>
    public string Name { get; set; } = string.Empty;
}
