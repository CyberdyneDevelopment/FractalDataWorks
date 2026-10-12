using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Commands.Data.Extensions;

/// <summary>
/// Builder for update commands with fluent filter construction.
/// The terminal method <see cref="Value"/> returns a <see cref="DataGatewayCall"/> that bundles
/// the address-free command with its <see cref="DataStoreTarget"/>.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public class UpdateCommandBuilder<T>
{
    private readonly string _containerName;
    private string? _dataStoreName;
    private string? _pathName;
    private readonly Stack<FilterGroupBuilder> _groupStack = new();
    private FilterGroupBuilder? _rootGroup;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCommandBuilder{T}"/> class.
    /// </summary>
    /// <param name="containerName">The container name.</param>
    public UpdateCommandBuilder(string containerName)
    {
        _containerName = containerName;
    }

    /// <summary>
    /// Specifies the DataStore name for container resolution (required).
    /// </summary>
    /// <param name="dataStoreName">The DataStore name.</param>
    /// <returns>The builder for method chaining.</returns>
    public UpdateCommandBuilder<T> DataStore(string dataStoreName)
    {
        _dataStoreName = dataStoreName ?? throw new ArgumentNullException(nameof(dataStoreName));
        return this;
    }

    /// <summary>
    /// Specifies the path name within the DataStore (e.g., schema name) (required).
    /// </summary>
    /// <param name="pathName">The path name.</param>
    /// <returns>The builder for method chaining.</returns>
    public UpdateCommandBuilder<T> Path(string pathName)
    {
        _pathName = pathName ?? throw new ArgumentNullException(nameof(pathName));
        return this;
    }

    /// <summary>
    /// Add a WHERE condition with a specific operator.
    /// </summary>
    public UpdateCommandBuilder<T> Where(string propertyName, IFilterOperator @operator, object? value)
    {
        var condition = new FilterCondition
        {
            PropertyName = propertyName,
            Operator = @operator,
            Value = value
        };

        if (_groupStack.Count > 0)
        {
            _groupStack.Peek().Nodes.Add(condition);
        }
        else
        {
            if (_rootGroup == null)
            {
                _rootGroup = new FilterGroupBuilder { Operator = LogicalOperator.And };
            }
            _rootGroup.Nodes.Add(condition);
        }

        return this;
    }

    /// <summary>
    /// Add an equality WHERE condition (operator = Equal).
    /// </summary>
    public UpdateCommandBuilder<T> Where(string propertyName, object? value)
    {
        return Where(propertyName, new EqualOperator(), value);
    }

    /// <summary>
    /// Begin a new AND group: (condition1 AND condition2 AND ...)
    /// Must call EndGroup() to close.
    /// </summary>
    public UpdateCommandBuilder<T> BeginAndGroup()
    {
        var newGroup = new FilterGroupBuilder { Operator = LogicalOperator.And };
        _groupStack.Push(newGroup);
        return this;
    }

    /// <summary>
    /// Begin a new OR group: (condition1 OR condition2 OR ...)
    /// Must call EndGroup() to close.
    /// </summary>
    public UpdateCommandBuilder<T> BeginOrGroup()
    {
        var newGroup = new FilterGroupBuilder { Operator = LogicalOperator.Or };
        _groupStack.Push(newGroup);
        return this;
    }

    /// <summary>
    /// End the current group started by BeginAndGroup() or BeginOrGroup().
    /// </summary>
    public UpdateCommandBuilder<T> EndGroup()
    {
        if (_groupStack.Count == 0)
        {
            throw new InvalidOperationException("No group to end. Call BeginAndGroup() or BeginOrGroup() first.");
        }

        var completedGroup = _groupStack.Pop();

        if (_groupStack.Count == 0)
        {
            if (_rootGroup == null)
            {
                _rootGroup = completedGroup;
            }
            else
            {
                _rootGroup.Nodes.Add(completedGroup.Build());
            }
        }
        else
        {
            _groupStack.Peek().Nodes.Add(completedGroup.Build());
        }

        return this;
    }

    /// <summary>
    /// Builds and returns a <see cref="DataGatewayCall"/> containing the update command
    /// and its <see cref="DataStoreTarget"/> address.
    /// </summary>
    /// <param name="entity">The entity data to update with.</param>
    /// <exception cref="InvalidOperationException">Thrown when DataStore or Path is not specified.</exception>
    public DataGatewayCall Value(T entity)
    {
        if (string.IsNullOrWhiteSpace(_dataStoreName))
            throw new InvalidOperationException("DataStore must be specified. Call DataStore() before Value().");
        if (string.IsNullOrWhiteSpace(_pathName))
            throw new InvalidOperationException("Path must be specified. Call Path() before Value().");

        IFilterExpression? filter = null;

        if (_rootGroup != null)
        {
            IFilterNode? root;

            if (_rootGroup.Nodes.Count == 1)
            {
                root = _rootGroup.Nodes[0];
            }
            else if (_rootGroup.Nodes.Count > 1)
            {
                root = _rootGroup.Build();
            }
            else
            {
                root = null;
            }

            if (root != null)
            {
                filter = new FilterExpression { Root = root };
            }
        }

        var command = new UpdateCommand<T>(entity) { Filter = filter };
        return new DataGatewayCall(command, new DataStoreTarget(_dataStoreName, _pathName, _containerName));
    }

    /// <summary>
    /// Mutable builder for FilterGroup during construction.
    /// </summary>
    private sealed class FilterGroupBuilder
    {
        public required LogicalOperator Operator { get; init; }
        public List<IFilterNode> Nodes { get; } = new();

        public FilterGroup Build()
        {
            return new FilterGroup
            {
                Operator = Operator,
                Nodes = Nodes
            };
        }
    }
}
