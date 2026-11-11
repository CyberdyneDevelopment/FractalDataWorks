using System;
using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions.Logging;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// A field of rows already read, known by its name and place alone (FDW-4911): a column of a file as the file carries it, which the
/// container may or may not declare.
/// </summary>
/// <remarks>
/// It states no type: the rows carry their own values. It is nullable because a row of a document may leave the field out. It is a leaf,
/// so asking it for a child node fails with <c>RecordFieldHasNoChild</c>.
/// </remarks>
public sealed class RecordField : IDataField
{
    /// <summary>Initializes a new instance of the <see cref="RecordField"/> class.</summary>
    /// <param name="name">The field's name, as the rows carry it.</param>
    /// <param name="ordinal">The field's place among the fields of the rows.</param>
    public RecordField(string name, int ordinal)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A field of the rows read has a name.", nameof(name));

        Name = name;
        Ordinal = ordinal;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string? Description => null;

    /// <inheritdoc />
    public IDataType? ExplicitType => null;

    /// <inheritdoc />
    public IFieldBinding? Binding => null;

    /// <inheritdoc />
    public int Ordinal { get; }

    /// <inheritdoc />
    public bool IsNullable => true;

    /// <inheritdoc />
    public IReadOnlyList<IDataNode> Nodes => [];

    /// <inheritdoc />
    public IGenericResult<IDataNode> Node(string name) =>
        GenericResult<IDataNode>.Failure(RowSourceLog.RecordFieldHasNoChild(NullLogger.Instance, Name, name));
}
