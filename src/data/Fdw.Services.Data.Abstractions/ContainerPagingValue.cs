namespace Fdw.Services.Data.Abstractions;

/// <summary>One paging value a container reads with, and where it was stated (FDW-5562).</summary>
/// <param name="Name">The setting's name, a key of the container's paging style.</param>
/// <param name="Value">The value the container reads with.</param>
/// <param name="Source">Where it was stated: <c>container</c>, <c>connection</c> or <c>style</c> (the style's declared default).</param>
public sealed record ContainerPagingValue(string Name, string Value, string Source);
