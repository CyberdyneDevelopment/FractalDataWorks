namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// The names a data gateway is asked for by (FDW-4395).
/// </summary>
/// <remarks>
/// Why a name at all: <see cref="IDataGatewayProvider.ByName"/> takes one, and the framework ships one gateway, "Main" (<c>MainDataGatewayImplementation</c>, the
/// <c>ServiceType</c> of the <c>dg.DataGateway</c> row). No container or store declares a gateway: the gateway routes by the <c>DataStoreTarget</c> each
/// command carries. Before this constant every store typed the string, and nothing would catch a typo.
/// </remarks>
public static class DataGatewayNames
{
    /// <summary>The name of the data gateway this framework ships, the one every store reaches its data through.</summary>
    public const string Main = "Main";
}
