using System;
using System.Collections.Generic;
using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources;

/// <summary>
/// Default implementation of row mapper provider.
/// </summary>
public sealed class RowMapperProvider : IRowMapperProvider
{
    private readonly Dictionary<string, IRowMapperFactory> _factories = new(StringComparer.OrdinalIgnoreCase);
    private string _defaultTypeName = "Pooled";

    /// <inheritdoc />
    public IRowMapperFactory? GetFactory(string typeName)
    {
        return _factories.TryGetValue(typeName, out var factory) ? factory : null;
    }

    /// <inheritdoc />
    public IRowMapperFactory GetDefaultFactory()
    {
        if (_factories.TryGetValue(_defaultTypeName, out var factory))
        {
            return factory;
        }

        // Return first available if default not found
        foreach (var kvp in _factories)
        {
            return kvp.Value;
        }

        // No factories registered - return a factory that creates empty mappers
        return new EmptyMapperFactory();
    }

    /// <inheritdoc />
    public void Register(string typeName, IRowMapperFactory factory)
    {
        _factories[typeName] = factory;
    }

    /// <summary>
    /// Sets the default mapper type name.
    /// </summary>
    /// <param name="typeName">The type name to use as default.</param>
    public void SetDefaultType(string typeName)
    {
        _defaultTypeName = typeName;
    }

    private sealed class EmptyMapperFactory : IRowMapperFactory
    {
        public IRowMapper Create()
        {
            return new EmptyRowMapper();
        }
    }

    private sealed class EmptyRowMapper : IRowMapper
    {
        public int EstimatedAllocationsPerRow => 1;
        public bool IsInitialized => false;

        public void Initialize(Fdw.Data.Abstractions.IStorageContainer container)
        {
            // No-op
        }

        public IDictionary<string, object?> MapRow(IRecordCursor source)
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        public void ReturnRow(IDictionary<string, object?> row)
        {
            // No-op for non-pooled
        }

        public void Reset()
        {
            // No-op
        }
    }
}
