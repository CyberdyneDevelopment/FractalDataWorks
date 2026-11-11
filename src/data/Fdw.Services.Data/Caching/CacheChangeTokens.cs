using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data.Caching;

/// <summary>
/// Where the cache change tokens live (FDW-4782): a container in the platform configuration store, which every host
/// declares in its configurationSchema.json. A host that does not declare it does not start.
/// </summary>
public static class CacheChangeTokens
{
    /// <summary>The path (schema) of the token container.</summary>
    public const string PathName = "data";

    /// <summary>The token container.</summary>
    public const string ContainerName = "CacheChangeToken";

    /// <summary>The token container in the platform configuration store.</summary>
    public static DataStoreTarget Target { get; } = new(ConfigurationGatewayTypes.ConfigurationConnection, PathName, ContainerName);
}
