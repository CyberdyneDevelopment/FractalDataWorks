using System;
using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Connections.RowQuery;

/// <summary>
/// The container's DECLARED field schema — the single authority the record-query pipeline validates
/// decoded rows, filter columns and join columns against. A container's declared fields are its
/// <see cref="IDataNode.Nodes"/> children (each an <see cref="IDataField"/> carrying
/// <see cref="IDataField.IsNullable"/>).
/// </summary>
/// <remarks>
/// Why one accessor: three steps of the pipeline need the same declared set (row validation, filter/join
/// column validation, and the reader's column list). Reading it in one place keeps them from drifting —
/// the reader must expose exactly the columns the rows were validated against.
/// </remarks>
internal static class ContainerFields
{
    /// <summary>
    /// Projects the container's child nodes to its declared <see cref="IDataField"/> schema.
    /// </summary>
    internal static IReadOnlyList<IDataField> Of(IDataContainer container)
    {
        var fields = new List<IDataField>(container.Nodes.Count);
        foreach (var node in container.Nodes)
        {
            fields.Add(node as IDataField
                ?? throw new InvalidOperationException(
                    $"Container '{container.Name}' child node '{node.Name}' is not an IDataField."));
        }

        return fields;
    }

    /// <summary>
    /// Gets the declared field names, in declaration order — the column set a
    /// <see cref="RecordDictionaryReader"/> exposes for the container's rows.
    /// </summary>
    internal static IReadOnlyList<string> Names(IDataContainer container)
    {
        var names = new List<string>(container.Nodes.Count);
        foreach (var node in container.Nodes)
        {
            names.Add(node.Name);
        }

        return names;
    }

    /// <summary>
    /// Determines whether the container declares a field with the supplied name.
    /// </summary>
    /// <remarks>
    /// Case-insensitive: decoded row dictionaries are keyed with
    /// <see cref="StringComparer.OrdinalIgnoreCase"/> (<c>DataRecord.ToDictionary</c>), and SQL column
    /// names are case-insensitive, so column identity must resolve the same way here.
    /// </remarks>
    internal static bool Declares(IDataContainer container, string columnName)
    {
        foreach (var node in container.Nodes)
        {
            if (string.Equals(node.Name, columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
