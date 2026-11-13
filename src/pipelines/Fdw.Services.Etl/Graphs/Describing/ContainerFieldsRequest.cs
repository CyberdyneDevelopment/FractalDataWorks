namespace Fdw.Services.Etl.Graphs.Describing;

/// <summary>Which container's fields a describe asks for (FDW-5523): what a ContainerSource component states.</summary>
/// <param name="Component">The component's name, for a failure's message.</param>
/// <param name="DataStore">The data store's name.</param>
/// <param name="Path">The path the component states, or none.</param>
/// <param name="Container">The container's name.</param>
public sealed record ContainerFieldsRequest(string Component, string DataStore, string? Path, string Container);
