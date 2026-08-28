using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Fdw.Collections;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Abstractions.Caching;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Fluent builder for QueryCommand to eliminate boilerplate and provide clean API.
/// Allows building complex queries with a chainable interface.
/// Supports hierarchical filter groups for complex WHERE clauses.
/// The terminal method <see cref="Build"/> returns a <see cref="DataGatewayCall"/> that bundles
/// the address-free command with its <see cref="DataStoreTarget"/>.
/// </summary>
/// <typeparam name="T">The result type from the query.</typeparam>
public class QueryCommandBuilder<T>
{
    private readonly string _dataStoreName;
    private readonly string? _pathName;
    private readonly string _containerName;

    private QueryCommand<T> _command;
    private readonly Stack<FilterGroupBuilder> _groupStack = new();
    private FilterGroupBuilder? _rootGroup;
    private readonly List<OrderedField> _orderedFields = [];
    private readonly Dictionary<string, object> _metadata = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<IJoinExpression> _joins = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryCommandBuilder{T}"/> class with full path specification (required).
    /// </summary>
    /// <param name="dataStoreName">The DataStore name for container resolution (required).</param>
    /// <param name="pathName">The path within the DataStore (e.g., schema name) (required).</param>
    /// <param name="containerName">The name of the container (table/endpoint) to query (required).</param>
    public QueryCommandBuilder(string dataStoreName, string pathName, string containerName)
    {
        _dataStoreName = dataStoreName;
        _pathName = pathName;
        _containerName = containerName;
        _command = new QueryCommand<T>();
    }

    /// <summary>
    /// Add a WHERE condition with a specific operator.
    /// If inside a group, adds to current group. Otherwise creates root-level condition.
    /// </summary>
    public QueryCommandBuilder<T> Where(string propertyName, IFilterOperator @operator, object? value)
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
    public QueryCommandBuilder<T> Where(string propertyName, object? value)
    {
        return Where(propertyName, new EqualOperator(), value);
    }

    /// <summary>
    /// Add a JOIN to a target container on a single column pair (e.g. child FK column → parent key column).
    /// </summary>
    /// <param name="targetContainer">The container/table to join to (e.g. the parent table name).</param>
    /// <param name="leftColumn">The column on the primary (FROM) container.</param>
    /// <param name="rightColumn">The column on the <paramref name="targetContainer"/>.</param>
    /// <param name="joinType">INNER (default), LEFT, RIGHT, or FULL.</param>
    /// <returns>This builder for chaining.</returns>
    public QueryCommandBuilder<T> Join(string targetContainer, string leftColumn, string rightColumn, string joinType = "INNER")
        => Join(targetContainer, [(leftColumn, rightColumn)], joinType);

    /// <summary>
    /// Add a JOIN to a target container on one or more column pairs (composite / natural keys).
    /// </summary>
    /// <param name="targetContainer">The container/table to join to.</param>
    /// <param name="conditions">Ordered (leftColumn, rightColumn) pairs combined with AND.</param>
    /// <param name="joinType">INNER (default), LEFT, RIGHT, or FULL.</param>
    /// <returns>This builder for chaining.</returns>
    public QueryCommandBuilder<T> Join(string targetContainer, IReadOnlyList<(string LeftColumn, string RightColumn)> conditions, string joinType = "INNER")
    {
        _joins.Add(new JoinExpression
        {
            TargetContainerName = targetContainer,
            JoinType = joinType,
            JoinConditions = conditions
        });
        return this;
    }

    /// <summary>
    /// Start a type-safe WHERE condition using a property expression.
    /// Returns a <see cref="FilterConditionBuilder{T, TProperty}"/> to select the operator.
    /// </summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertySelector">Expression selecting the property to filter on.</param>
    /// <returns>A condition builder for selecting the operator and value.</returns>
    /// <example>
    /// <code>
    /// // Type-safe property access with IntelliSense
    /// var call = DataQuery.From&lt;Customer&gt;("CRM", "sales", "Customers")
    ///     .Where(c => c.Name).Equal("Acme")
    ///     .Where(c => c.Age).GreaterThan(18)
    ///     .Where(c => c.Status).In("Active", "Pending")
    ///     .Build();
    /// </code>
    /// </example>
    public FilterConditionBuilder<T, TProperty> Where<TProperty>(Expression<Func<T, TProperty>> propertySelector)
    {
        var propertyName = ExpressionHelper.ExtractPropertyName(propertySelector);
        return new FilterConditionBuilder<T, TProperty>(this, propertyName);
    }

    /// <summary>
    /// Begin a new AND group: (condition1 AND condition2 AND ...)
    /// Must call EndGroup() to close.
    /// </summary>
    public QueryCommandBuilder<T> BeginAndGroup()
    {
        var newGroup = new FilterGroupBuilder { Operator = LogicalOperator.And };
        _groupStack.Push(newGroup);
        return this;
    }

    /// <summary>
    /// Begin a new OR group: (condition1 OR condition2 OR ...)
    /// Must call EndGroup() to close.
    /// </summary>
    public QueryCommandBuilder<T> BeginOrGroup()
    {
        var newGroup = new FilterGroupBuilder { Operator = LogicalOperator.Or };
        _groupStack.Push(newGroup);
        return this;
    }

    /// <summary>
    /// End the current group started by BeginAndGroup() or BeginOrGroup().
    /// </summary>
    public QueryCommandBuilder<T> EndGroup()
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
    /// Add an ORDER BY clause in ascending order.
    /// </summary>
    /// <param name="propertyName">The property/column name to order by.</param>
    /// <returns>This builder for chaining.</returns>
    public QueryCommandBuilder<T> OrderBy(string propertyName)
    {
        return OrderBy(propertyName, SortDirections.ByName("Ascending"));
    }

    /// <summary>
    /// Add an ORDER BY clause with specified direction.
    /// </summary>
    /// <param name="propertyName">The property/column name to order by.</param>
    /// <param name="direction">The sort direction (Ascending or Descending).</param>
    /// <returns>This builder for chaining.</returns>
    public QueryCommandBuilder<T> OrderBy(string propertyName, ISortDirection direction)
    {
        _orderedFields.Add(new OrderedField
        {
            PropertyName = propertyName,
            Direction = direction
        });
        return this;
    }

    /// <summary>
    /// Add a descending ORDER BY clause.
    /// </summary>
    /// <param name="propertyName">The property/column name to order by.</param>
    /// <returns>This builder for chaining.</returns>
    public QueryCommandBuilder<T> OrderByDescending(string propertyName)
    {
        return OrderBy(propertyName, SortDirections.ByName("Descending"));
    }

    /// <summary>
    /// Add a type-safe ORDER BY clause in ascending order.
    /// </summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertySelector">Expression selecting the property to order by.</param>
    /// <returns>This builder for chaining.</returns>
    public QueryCommandBuilder<T> OrderBy<TProperty>(Expression<Func<T, TProperty>> propertySelector)
    {
        var propertyName = ExpressionHelper.ExtractPropertyName(propertySelector);
        return OrderBy(propertyName);
    }

    /// <summary>
    /// Add a type-safe descending ORDER BY clause.
    /// </summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertySelector">Expression selecting the property to order by.</param>
    /// <returns>This builder for chaining.</returns>
    public QueryCommandBuilder<T> OrderByDescending<TProperty>(Expression<Func<T, TProperty>> propertySelector)
    {
        var propertyName = ExpressionHelper.ExtractPropertyName(propertySelector);
        return OrderByDescending(propertyName);
    }

    /// <summary>
    /// Add SKIP and TAKE for pagination.
    /// </summary>
    /// <param name="skip">Number of rows to skip.</param>
    /// <param name="take">Number of rows to take.</param>
    /// <returns>This builder for chaining.</returns>
    public QueryCommandBuilder<T> Paging(int skip = 0, int take = 1000)
    {
        var newCommand = new QueryCommand<T>
        {
            Metadata = new Dictionary<string, object>(_metadata, StringComparer.OrdinalIgnoreCase),
            Paging = new PagingExpression { Skip = skip, Take = take }
        };
        _command = newCommand;
        return this;
    }

    /// <summary>
    /// Add a metadata key-value pair to the command.
    /// Metadata is propagated through all command rebuilds during Build().
    /// </summary>
    /// <param name="key">The metadata key (case-insensitive).</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>This builder for chaining.</returns>
    public QueryCommandBuilder<T> WithMetadata(string key, object value)
    {
        _metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Enable caching for this query's result at the DataGateway level.
    /// Only effective when a <see cref="CachePolicy"/>-aware decorator is registered.
    /// </summary>
    /// <param name="duration">Cache duration. Null uses the decorator's default (5 minutes).</param>
    /// <param name="invalidationTags">
    /// Tags that writers use to invalidate this cache entry.
    /// Null auto-derives from "{PathName}.{ContainerName}" at Build time.
    /// </param>
    /// <returns>This builder for chaining.</returns>
    public QueryCommandBuilder<T> WithCaching(TimeSpan? duration = null, params string[] invalidationTags)
    {
        _metadata[CachePolicy.CacheEnabledKey] = true;

        if (duration.HasValue)
            _metadata[CachePolicy.CacheDurationKey] = duration.Value;

        if (invalidationTags.Length > 0)
            _metadata[CachePolicy.CacheInvalidationTagsKey] = invalidationTags;

        return this;
    }

    /// <summary>
    /// Builds and returns a <see cref="DataGatewayCall"/> containing the completed query command
    /// and its <see cref="DataStoreTarget"/> address.
    /// </summary>
    public DataGatewayCall Build()
    {
        var command = _command;

        // Apply filter if we have a root group
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
                var filter = new FilterExpression { Root = root };
                var newCommand = new QueryCommand<T>
                {
                    Metadata = new Dictionary<string, object>(_metadata, StringComparer.OrdinalIgnoreCase),
                    Filter = filter,
                    Paging = command.Paging
                };
                command = newCommand;
            }
        }

        // Apply ordering if specified
        if (_orderedFields.Count > 0)
        {
            var ordering = new OrderingExpression { OrderedFields = _orderedFields };
            var newCommand = new QueryCommand<T>
            {
                Metadata = new Dictionary<string, object>(_metadata, StringComparer.OrdinalIgnoreCase),
                Filter = command.Filter,
                Ordering = ordering,
                Paging = command.Paging
            };
            command = newCommand;
        }

        if (_metadata.Count > 0 && command == _command)
        {
            command = new QueryCommand<T>
            {
                Metadata = new Dictionary<string, object>(_metadata, StringComparer.OrdinalIgnoreCase),
                Filter = command.Filter,
                Ordering = command.Ordering,
                Paging = command.Paging
            };
        }

        if (_joins.Count > 0 && command.Joins.Count == 0)
        {
            command = new QueryCommand<T>
            {
                Metadata = new Dictionary<string, object>(_metadata, StringComparer.OrdinalIgnoreCase),
                Filter = command.Filter,
                Projection = command.Projection,
                Ordering = command.Ordering,
                Paging = command.Paging,
                Aggregation = command.Aggregation,
                Joins = _joins
            };
        }

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
