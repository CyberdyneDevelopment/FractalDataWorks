namespace Fdw.Services.Calculations;

/// <summary>
/// Reference to a connection and container path for deferred data resolution.
/// </summary>
/// <param name="ConnectionName">The connection name.</param>
/// <param name="ContainerPath">The container path within the connection.</param>
public sealed record ContainerReference(string ConnectionName, string ContainerPath);
