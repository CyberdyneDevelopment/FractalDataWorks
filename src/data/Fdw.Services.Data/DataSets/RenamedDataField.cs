using System;
using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Data;

/// <summary>
/// A store field under the data set's logical name (FDW-3629): everything the store said about the field, its type, binding, ordinal,
/// nullability and children, with only the name changed.
/// </summary>
internal sealed class RenamedDataField : IDataField
{
    private readonly IDataField _field;

    /// <summary>Initializes a new instance of the <see cref="RenamedDataField"/> class.</summary>
    /// <param name="field">The field as the store names it.</param>
    /// <param name="name">The data set's logical name for it.</param>
    public RenamedDataField(IDataField field, string name)
    {
        _field = field;
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("The logical name must be given.", nameof(name)) : name;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string? Description => _field.Description;

    /// <inheritdoc />
    public IReadOnlyList<IDataNode> Nodes => _field.Nodes;

    /// <inheritdoc />
    public IDataType? ExplicitType => _field.ExplicitType;

    /// <inheritdoc />
    public IFieldBinding? Binding => _field.Binding;

    /// <inheritdoc />
    public int Ordinal => _field.Ordinal;

    /// <inheritdoc />
    public bool IsNullable => _field.IsNullable;

    /// <inheritdoc />
    public IGenericResult<IDataNode> Node(string name) => _field.Node(name);
}
