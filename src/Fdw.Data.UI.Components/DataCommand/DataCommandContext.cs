using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.UI.CommandBuilders.Abstractions;
using Fdw.UI.Providers;

namespace Fdw.Data.UI.Components.DataCommand;

/// <summary>
/// State shape for the <c>DataCommandProvider</c> headless component.
/// Passed to command-kind skin components via <c>ChildContent</c> or a Parameter.
/// Implements <see cref="ICommandBuilderContext{TCommandSpec}"/> so Builder.razor can
/// interact with it through the shared abstraction.
/// </summary>
public sealed class DataCommandContext : ProviderContextBase, ICommandBuilderContext<DataCommandSpec>
{
    /// <inheritdoc />
    public DataCommandSpec CurrentSpec { get; init; } = new DataCommandSpec();

    /// <inheritdoc />
    public string CurrentKind { get; init; } = "Query";



    /// <inheritdoc />
    public Func<DataCommandSpec, Task> OnChanged { get; init; } = _ => Task.CompletedTask;

    // ── Metadata ──────────────────────────────────────────────────────────────

    /// <summary>
    /// All containers available to the current connection or DataSet.
    /// Loaded by <c>DataCommandProvider</c> from data.DataContainer metadata.
    /// </summary>
    public IReadOnlyList<DataContainerSummary> AvailableContainers { get; init; } = [];

    /// <summary>
    /// Returns the fields for the given container, or an empty list if the container
    /// is not found or has no fields.
    /// </summary>
    /// <param name="containerId">The container's unique identifier.</param>
    /// <returns>Field list, possibly empty — never null.</returns>
    public IReadOnlyList<DataFieldSummary> GetFields(Guid containerId)
    {
        foreach (var c in AvailableContainers)
        {
            if (c.Id == containerId)
                return c.Fields;
        }
        return [];
    }

    // ── Callbacks (invoked by skins) ──────────────────────────────────────────

    /// <summary>Callback for the skin to set the command kind (e.g., switching from Query to Insert).</summary>
    public Func<string, Task> SetKind { get; init; } = _ => Task.CompletedTask;

    /// <summary>Callback for the skin to update the FROM container in a Query spec.</summary>
    public Func<Guid, string, Task> SetFromContainer { get; init; } = (_, _) => Task.CompletedTask;

    /// <summary>Callback to add a join clause to a Query spec.</summary>
    public Func<DataCommandJoin, Task> AddJoin { get; init; } = _ => Task.CompletedTask;

    /// <summary>Callback to remove a join clause by index from a Query spec.</summary>
    public Func<int, Task> RemoveJoin { get; init; } = _ => Task.CompletedTask;

    /// <summary>Callback to update the full field list on the current spec.</summary>
    public Func<IReadOnlyList<DataCommandField>, Task> SetFields { get; init; } = _ => Task.CompletedTask;

    /// <summary>Callback to set the filter expression on the current spec.</summary>
    public Func<DataCommandFilter?, Task> SetFilter { get; init; } = _ => Task.CompletedTask;

    /// <summary>Callback to set paging on a Query spec.</summary>
    public Func<DataCommandPaging?, Task> SetPaging { get; init; } = _ => Task.CompletedTask;

    /// <summary>Callback to set the sort list on a Query spec.</summary>
    public Func<IReadOnlyList<DataCommandSort>, Task> SetSort { get; init; } = _ => Task.CompletedTask;

    /// <summary>Callback to set the write-command target container.</summary>
    public Func<Guid, Task> SetTarget { get; init; } = _ => Task.CompletedTask;

    /// <summary>Callback to update the SET clause list for Update/Upsert specs.</summary>
    public Func<IReadOnlyList<DataCommandSetClause>, Task> SetSetClauses { get; init; } = _ => Task.CompletedTask;

    /// <summary>Callback to update the VALUES list for Insert specs.</summary>
    public Func<IReadOnlyList<DataCommandValueEntry>, Task> SetValues { get; init; } = _ => Task.CompletedTask;

    /// <summary>Callback to update the key fields list for Upsert/BulkUpsert specs.</summary>
    public Func<IReadOnlyList<string>, Task> SetKeyFields { get; init; } = _ => Task.CompletedTask;

    /// <summary>Callback to set the batch size for BulkInsert/BulkUpsert specs.</summary>
    public Func<int, Task> SetBatchSize { get; init; } = _ => Task.CompletedTask;
}
