using System.Collections.Generic;

namespace Fdw.Services.Data.Abstractions;

/// <summary>The paging a container reads with, its own settings laid over its connection's (FDW-5562), and where each value came from.</summary>
/// <param name="Style">The name of the paging style the container is read in.</param>
/// <param name="StyleSource">Where the style was stated: <c>container</c> or <c>connection</c>.</param>
/// <param name="Settings">Every value the style reads, in the order the style declares them: the stated ones, and the page size and page count the style declares a default for.</param>
public sealed record ContainerPagingView(string Style, string StyleSource, IReadOnlyList<ContainerPagingValue> Settings);
