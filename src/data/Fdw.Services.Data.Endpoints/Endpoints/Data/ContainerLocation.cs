using Fdw.Services.Connections;

namespace Fdw.Services.Data.Endpoints;

/// <summary>A container found in a data store, with the store it was found in (FDW-5562).</summary>
/// <param name="Store">The data store, read with its paths and containers.</param>
/// <param name="Container">The container.</param>
internal sealed record ContainerLocation(IDataStoreImplementationConfiguration Store, DataContainerConfiguration Container);
