using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Lists the data stores, their paths, containers and container fields as catalog entries (FDW-1681).</summary>
/// <remarks>The data store provider answers each store as one tree, so a single read serves all four kinds.</remarks>
internal static class DataStoreEntries
{
    /// <summary>Walks every data store and lists the nodes of the kinds asked for.</summary>
    /// <param name="provider">The data store provider.</param>
    /// <param name="kinds">The kinds the search was asked for.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The entries, or the failure of the read.</returns>
    internal static async Task<IGenericResult<List<CatalogEntryDto>>> Read(
        IDataStoreProvider provider, CatalogKindSet kinds, CancellationToken cancellationToken)
    {
        var read = await provider.Get(cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
        {
            return read.ToNewResult<List<CatalogEntryDto>>();
        }

        var entries = new List<CatalogEntryDto>();
        foreach (var store in read.Value!)
        {
            Add(entries, kinds, CatalogKinds.DataStore, store, []);
            foreach (var path in store.Paths)
            {
                Add(entries, kinds, CatalogKinds.DataPath, path, [store.Name]);
                foreach (var container in path.Containers)
                {
                    AddContainer(entries, kinds, container, [store.Name, path.Name]);
                }
            }
        }

        return GenericResult<List<CatalogEntryDto>>.Success(entries);
    }

    private static void AddContainer(List<CatalogEntryDto> entries, CatalogKindSet kinds, IDataContainer container, string[] parents)
    {
        Add(entries, kinds, CatalogKinds.Container, container, parents);
        foreach (var field in container.Nodes)
        {
            Add(entries, kinds, CatalogKinds.ContainerField, field, [.. parents, container.Name]);
        }
    }

    // Why both: the parents are for a screen that opens the entity, the location is the same names as one line for a person to read.
    private static void Add(List<CatalogEntryDto> entries, CatalogKindSet kinds, string kind, IDataNode node, string[] parents)
    {
        if (!kinds.Wants(kind))
        {
            return;
        }

        entries.Add(new CatalogEntryDto
        {
            EntityType = kind,
            Name = node.Name,
            Description = node.Description,
            Location = parents.Length == 0 ? null : string.Join(" / ", parents),
            Parents = parents,
        });
    }
}
