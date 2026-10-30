using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Configuration;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Results;
using Fdw.Services.Configuration.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Configuration;

/// <summary>
/// Says which of a record's child lists is a tree: a list whose elements are of the record's own type (FDW-3004).
/// </summary>
/// <remarks>
/// A child list is read from a table that hangs from its owner's row. The elements of a tree are not child rows: each is a member of the
/// same domain, with a name and an identity of its own, and it hangs from its parent through the implementation's Foreign key to the
/// domain's durable Id (a node's <c>ParentId</c>). The implementation provider therefore leaves such a list alone when it loads, saves
/// and retires a record, and the domain provider, which knows a member's identity, attaches it.
/// </remarks>
internal static class ConfigurationTrees
{
    /// <summary>Says whether a child list is a tree of its owner.</summary>
    /// <param name="descriptor">The child list the owner's mapper declares.</param>
    /// <param name="ownerType">The type of the record that holds the list.</param>
    /// <returns>True when the list holds records of the owner's own type; a name/value collection never does.</returns>
    internal static bool IsTree(IChildCascadeDescriptor descriptor, Type ownerType)
        => !descriptor.IsPropertyCollection && descriptor.ChildType == ownerType;

    /// <summary>Hangs every member of a domain under its parent, from a record down to <paramref name="levels"/> levels.</summary>
    /// <param name="root">The record whose tree lists are filled.</param>
    /// <param name="members">Every member of the domain, composed, the root's own copy included.</param>
    /// <param name="linkField">The field of a member that holds its parent's durable Id; a member whose field is null is a root.</param>
    /// <param name="trees">The record's tree lists.</param>
    /// <param name="mapper">The record's mapper, which reads a member's fields and makes a typed list.</param>
    /// <param name="levels">How many levels down to hang; a level is one hop from parent to child.</param>
    /// <param name="logger">The logger a failure is written to.</param>
    /// <returns>Success, or the failure that names the member where the links are wrong.</returns>
    internal static IGenericResult Attach(
        IImplementationConfiguration root,
        IReadOnlyList<IImplementationConfiguration> members,
        string linkField,
        List<IChildCascadeDescriptor> trees,
        IPocoMapper mapper,
        int levels,
        ILogger logger)
    {
        var byParent = new Dictionary<Guid, List<IImplementationConfiguration>>();
        foreach (var member in members)
        {
            if (!mapper.MapToParameters(member).TryGetValue(linkField, out var parent))
                return GenericResult.Failure(DefaultConfigurationProviderLog.TreeLinkNotOnRecord(logger, member.GetType().Name, linkField));

            if (parent is not Guid parentId)
                continue;

            if (!byParent.TryGetValue(parentId, out var siblings))
            {
                siblings = [];
                byParent[parentId] = siblings;
            }

            siblings.Add(member);
        }

        return Hang(root, byParent, trees, mapper, [], levels, logger);
    }

    // One member's children, then theirs. The ancestors on the way down are tracked so a link that leads back to one fails
    // here, naming the member, and not as a stack overflow.
    private static IGenericResult Hang(
        IImplementationConfiguration parent,
        Dictionary<Guid, List<IImplementationConfiguration>> byParent,
        List<IChildCascadeDescriptor> trees,
        IPocoMapper mapper,
        HashSet<Guid> ancestors,
        int levels,
        ILogger logger)
    {
        if (levels <= 0)
            return GenericResult.Success();

        if (!ancestors.Add(parent.Id))
            return GenericResult.Failure(DefaultConfigurationProviderLog.TreeCycle(logger, parent.GetType().Name, parent.Name));

        var children = byParent.TryGetValue(parent.Id, out var found) ? found : new List<IImplementationConfiguration>();
        for (var i = 0; i < trees.Count; i++)
        {
            var list = mapper.CreateList();
            foreach (var child in children)
                list.Add(child);

            trees[i].SetCollection(parent, list);
        }

        foreach (var child in children)
        {
            var below = Hang(child, byParent, trees, mapper, ancestors, levels - 1, logger);
            if (below.IsFailure)
                return below;
        }

        ancestors.Remove(parent.Id);
        return GenericResult.Success();
    }
}
