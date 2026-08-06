using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Configuration.SourceGenerators.Models;

namespace Fdw.Configuration.SourceGenerators.Analysis;

/// <summary>
/// Represents a parent/child relationship graph.
/// </summary>
public class ParentChildGraph
{
    private readonly Dictionary<string, ConfigurationModel> _configsByName =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, List<string>> _childrenByParent =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Adds a configuration to the graph.
    /// </summary>
    public void AddConfiguration(ConfigurationModel config)
    {
        _configsByName[config.ClassName] = config;
    }

    /// <summary>
    /// Adds a parent/child relationship.
    /// </summary>
    public void AddRelationship(string parentName, string childName)
    {
        if (!_childrenByParent.TryGetValue(parentName, out var children))
        {
            children = new List<string>();
            _childrenByParent[parentName] = children;
        }

        if (!children.Contains(childName, StringComparer.OrdinalIgnoreCase))
        {
            children.Add(childName);
        }
    }

    /// <summary>
    /// Gets a configuration by name.
    /// </summary>
    public ConfigurationModel? Get(string name)
    {
        return _configsByName.TryGetValue(name, out var config) ? config : null;
    }

    /// <summary>
    /// Gets child configuration names for a parent.
    /// </summary>
    public IEnumerable<string> GetChildNames(string parentName)
    {
        return _childrenByParent.TryGetValue(parentName, out var children)
            ? children
            : Enumerable.Empty<string>();
    }

    /// <summary>
    /// Gets child configurations for a parent.
    /// </summary>
    public IEnumerable<ConfigurationModel> GetChildren(string parentName)
    {
        foreach (var childName in GetChildNames(parentName))
        {
            if (_configsByName.TryGetValue(childName, out var child))
            {
                yield return child;
            }
        }
    }

    /// <summary>
    /// Gets all root configurations (those without a parent).
    /// </summary>
    /// <remarks>
    /// Why: ParentTableName was removed from ConfigurationModel in FDW-395 Phase 6.
    /// All configurations are now root-level (IDataNode owns hierarchy).
    /// </remarks>
    public IEnumerable<ConfigurationModel> GetRoots()
    {
        return _configsByName.Values;
    }

    /// <summary>
    /// Gets all configurations.
    /// </summary>
    public IEnumerable<ConfigurationModel> GetAll()
    {
        return _configsByName.Values;
    }

    /// <summary>
    /// Checks if a configuration has children.
    /// </summary>
    public bool HasChildren(string parentName)
    {
        return _childrenByParent.TryGetValue(parentName, out var children) && children.Count > 0;
    }
}