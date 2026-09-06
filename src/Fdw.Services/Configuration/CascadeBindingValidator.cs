using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Results;
using Fdw.Services.Configuration.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Configuration;

/// <summary>
/// Checks that every typed-list cascade child a generated mapper declares can actually be resolved
/// to a configuration command.
/// </summary>
/// <remarks>
/// A missing command is otherwise found one child at a time, whenever some configuration that owns
/// it happens to be loaded, which can be long after the change that caused it. Run this once at
/// Initialize, after the collections are frozen, and every gap is named in a single failure.
/// </remarks>
public static class CascadeBindingValidator
{
    /// <summary>Names every typed-list cascade child that no configuration command can resolve.</summary>
    /// <param name="logger">The logger to write the failure to.</param>
    /// <returns>Success when every declared child resolves; otherwise a failure naming all of them.</returns>
    public static IGenericResult Validate(ILogger logger)
    {
        var unresolved = new List<string>();

        foreach (var mapper in PocoMapperCollection.All())
        {
            var children = mapper.CascadeChildren;
            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];

                // A key/value child declares its own container and has no configuration type to look up.
                if (child.IsPropertyCollection)
                    continue;

                if (ConfigurationCommands.ByType(child.ChildType) != ConfigurationCommands.NotFound)
                    continue;

                unresolved.Add(
                    $"{mapper.TargetType.Name}.{child.BoundPropertyName} (child type '{child.ChildTypeName}')");
            }
        }

        if (unresolved.Count == 0)
            return GenericResult.Success();

        return GenericResult.Failure(
            DefaultConfigurationProviderLog.CascadeChildrenWithoutCommands(
                logger, unresolved.Count, string.Join("; ", unresolved.OrderBy(x => x, System.StringComparer.Ordinal))));
    }
}
