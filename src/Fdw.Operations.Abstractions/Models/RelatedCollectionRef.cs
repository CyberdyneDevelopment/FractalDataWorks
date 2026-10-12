namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Describes a TypeCollection that provides valid values for a specific property on a configuration type.
/// Populated from [ValuesFrom] attributes on the configuration class.
/// </summary>
public sealed class RelatedCollectionRef
{
    /// <summary>
    /// Gets or sets the property name on the configuration class that this collection drives.
    /// For example, "AuthenticationType" on MsSqlConnectionConfiguration.
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the TypeCollection name to query for valid values.
    /// For example, "MsSqlAuthenticationTypes".
    /// </summary>
    public string CollectionName { get; set; } = string.Empty;
}
