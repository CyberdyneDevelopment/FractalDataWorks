using System;
using System.Net.Http.Headers;

namespace Fdw.Services.Connections.Http.Paging;

/// <summary>What one page of a paged HTTP read returned, for its paging plan to find the next page from (FDW-4938).</summary>
/// <param name="Page">The page's number in this read, from 1.</param>
/// <param name="Request">The absolute address the page was asked for at.</param>
/// <param name="Rows">The rows the page held.</param>
/// <param name="Headers">The response's headers.</param>
/// <param name="Body">The page's body, when the style reads it (<see cref="HttpPagingPlan.ReadsBody"/>); empty otherwise.</param>
public sealed record HttpPageOutcome(int Page, Uri Request, int Rows, HttpResponseHeaders Headers, ReadOnlyMemory<byte> Body);
