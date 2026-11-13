namespace Fdw.Services.Data.Endpoints;

/// <summary>One paging value a container reads with, and where it was stated (FDW-5562).</summary>
public sealed class ContainerPagingValueDto
{
    /// <summary>Gets or sets the setting's name, a key of the container's paging style.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the value the container reads with.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Gets or sets where the value was stated: <c>container</c>, <c>connection</c>, or <c>style</c> for the style's declared default.</summary>
    public string Source { get; set; } = string.Empty;
}
