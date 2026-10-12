using System.Collections.Generic;

namespace Fdw.Services.Workspaces.Charts;

/// <summary>What a saved chart's <c>Encoding</c> binds: for each slot that has one, the bindings on it.</summary>
/// <param name="Bindings">The bound slots by name, ignoring case. A slot bound to nothing is absent.</param>
public sealed record ChartEncoding(IReadOnlyDictionary<string, IReadOnlyList<ChartBinding>> Bindings);
