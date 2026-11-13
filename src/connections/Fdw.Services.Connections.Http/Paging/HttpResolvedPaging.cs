using System.Collections.Generic;

namespace Fdw.Services.Connections.Http.Paging;

/// <summary>The paging an HTTP read follows, once the container's own settings have been laid over its connection's (FDW-5562).</summary>
/// <param name="Style">The paging style the read is made in.</param>
/// <param name="StyleSource">Where the style was stated: <see cref="HttpPagingSources.Container"/> or <see cref="HttpPagingSources.Connection"/>.</param>
/// <param name="Settings">The effective settings, holding only the keys the style reads that the container or the connection states.</param>
/// <param name="Sources">Where each stated key came from, by key, ignoring case: <see cref="HttpPagingSources.Container"/> or <see cref="HttpPagingSources.Connection"/>.</param>
public sealed record HttpResolvedPaging(
    HttpPagingStyleBase Style,
    string StyleSource,
    HttpPagingSettings Settings,
    IReadOnlyDictionary<string, string> Sources);
