using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Http.Paging;

/// <summary>What a paging style builds a read's plan from (FDW-4938): the connection it reads and the additional properties it states.</summary>
/// <param name="ConnectionName">The HTTP connection's name, for the messages.</param>
/// <param name="Values">The connection's additional properties, by name, ignoring case.</param>
/// <param name="Logger">The logger refusals are written to.</param>
public sealed record HttpPagingSettings(string ConnectionName, IReadOnlyDictionary<string, string?> Values, ILogger Logger);
