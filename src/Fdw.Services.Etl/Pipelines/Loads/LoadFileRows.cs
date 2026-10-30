using System.Collections.Generic;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>The rows that go to one file of a Files destination (FDW-3193).</summary>
/// <param name="FileName">The file name: the split value with the extension of the declared file.</param>
/// <param name="Rows">The rows whose split value names this file.</param>
public sealed record LoadFileRows(string FileName, IReadOnlyList<IDictionary<string, object?>> Rows);
