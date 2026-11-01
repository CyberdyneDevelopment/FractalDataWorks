using System.Collections.Generic;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>What one step of an expansion answered, handed to the command's <c>Assemble</c> in the order the steps ran.</summary>
/// <param name="Affected">The rows the step affected; zero for a step that answers rows.</param>
/// <param name="Rows">The rows the step answered, one dictionary each, or <see langword="null"/> for a step that answers a count.</param>
public sealed record ExpansionAnswer(int Affected, IReadOnlyList<IReadOnlyDictionary<string, object?>>? Rows = null);
