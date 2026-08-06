namespace Fdw.Operations.Endpoints.ConfigurationMetadata;

/// <summary>
/// Describes a TypeCollection that provides valid values for a specific property on a configuration type.
/// </summary>
public sealed class RelatedCollectionRefDto
{
    /// <summary>Gets or sets the property name on the configuration class (e.g., "AuthenticationType").</summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>Gets or sets the TypeCollection name to query (e.g., "MsSqlAuthenticationTypes").</summary>
    public string CollectionName { get; set; } = string.Empty;
}
