using System.Collections.Generic;

namespace Fdw.Services.Connections.Http.Paging;

/// <summary>
/// How one request asks a remote for the window of rows a query names (FDW-4938): the query parameters the connection's paging style writes,
/// and what the reader cuts from the response itself when the remote cannot be asked.
/// </summary>
/// <param name="Parameters">The query parameters, by the names the connection states, and their values.</param>
/// <param name="SkipHere">The rows at the start of the response the reader drops (a remote that does not page); 0 otherwise.</param>
/// <param name="Take">The most rows the reader keeps; null for all.</param>
public sealed record HttpPagingWindow(IReadOnlyList<(string Name, string Value)> Parameters, int SkipHere, int? Take);
