using System;
using System.Linq;
using Fdw.Operations.Abstractions.TypeCollections.Execution;

namespace Fdw.Operations.Endpoints.Executions;

/// <summary>
/// Says which item type a list of executions holds (FDW-2033). A stage is a number on a component and not a run to list, so a list with no stated
/// item type holds runs (Job items) only.
/// </summary>
/// <remarks>
/// Why a caller who names a root or a correlation id gets every type: that caller asks for the whole family of one run, and its stages, steps and tests
/// are part of it. Why an unknown item type is refused: showing everything for a name nothing matches would hide the mistake.
/// </remarks>
public static class ExecutionListItemType
{
    /// <summary>Chooses the item type a list holds.</summary>
    /// <param name="itemType">The item type the caller named, or none.</param>
    /// <param name="hasRoot">Whether the caller named a root id.</param>
    /// <param name="hasCorrelation">Whether the caller named a correlation id.</param>
    /// <param name="refusal">Receives why the named item type is not accepted; null otherwise.</param>
    /// <returns>The item type to list, or <see langword="null"/> for every type.</returns>
    public static IExecutionItemType? Of(string? itemType, bool hasRoot, bool hasCorrelation, out string? refusal)
    {
        refusal = null;
        if (!string.IsNullOrWhiteSpace(itemType))
        {
            // Why the name is matched here and not by ByName: a collection's names are canonical and ByName is exact, while this is text a caller typed.
            var named = ExecutionItemTypes.All().FirstOrDefault(type => string.Equals(type.Name, itemType, StringComparison.OrdinalIgnoreCase));
            if (named is null)
            {
                refusal = $"'{itemType}' is not an execution item type; the types are {string.Join(", ", ExecutionItemTypes.All().Select(type => type.Name))}";
                return null;
            }

            return named;
        }

        return hasRoot || hasCorrelation ? null : ExecutionItemTypes.ByName("Job");
    }
}
