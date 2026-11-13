using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The paging a container reads with: its own style and settings laid over its connection's, and where each value came from (FDW-5562).</summary>
/// <remarks>
/// Worked out by the API, so a console holds no resolution rule and shows the values a container inherits. Absent on a container of a store whose containers do
/// not page, and on one whose connection states no style and which states none itself.
/// </remarks>
public sealed class ContainerPagingDto
{
    /// <summary>Gets or sets the name of the paging style the container is read in.</summary>
    public string Style { get; set; } = string.Empty;

    /// <summary>Gets or sets where the style was stated: <c>container</c> or <c>connection</c>.</summary>
    public string StyleSource { get; set; } = string.Empty;

    /// <summary>Gets or sets every value the style reads, in the order the style declares them: the stated ones, and the page size and page count the style declares a default for.</summary>
    public IList<ContainerPagingValueDto> Settings { get; set; } = [];
}
