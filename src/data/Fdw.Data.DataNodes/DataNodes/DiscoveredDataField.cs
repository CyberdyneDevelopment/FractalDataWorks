using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Schema;
using Fdw.Schema.Properties;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.DataNodes;

/// <summary>
/// A column a schema discovery read, as a leaf <see cref="IDataNode"/> a key can name: an <see cref="IDataField"/> that is also the
/// <see cref="IField"/> the discovery produced, answering every <see cref="IField"/> member from it.
/// </summary>
/// <remarks>
/// Why this exists (FDW-6309): <see cref="IContainerKeyField.LocalField"/> is an <see cref="IDataField"/>, and a discovered column
/// was only an <see cref="IField"/>. <see cref="DataField"/> cannot stand in, because it answers false to identity, computed and
/// system-provided and carries no native type, and the data store merge reads exactly those from the discovered column.
/// </remarks>
public sealed class DiscoveredDataField : IDataField, IField
{
    private readonly IField _field;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoveredDataField"/> class.
    /// </summary>
    /// <param name="field">The column the discovery produced; every <see cref="IField"/> answer comes from it.</param>
    /// <param name="ordinal">The column's one-based position in its container.</param>
    public DiscoveredDataField(IField field, int ordinal)
    {
        _field = field;
        Ordinal = ordinal;
    }

    /// <inheritdoc />
    public string Name => _field.Name;

    /// <inheritdoc />
    public string? Description => _field.Description;

    /// <inheritdoc />
    public IDataType? ExplicitType => _field.ExplicitType;

    /// <inheritdoc />
    public IFieldBinding? Binding => null;

    /// <inheritdoc />
    public int Ordinal { get; }

    /// <inheritdoc />
    public bool IsNullable => _field.IsNullable;

    /// <inheritdoc />
    public IReadOnlyList<IDataNode> Nodes => [];

    /// <inheritdoc />
    public IGenericResult<IDataNode> Node(string name) =>
        GenericResult<IDataNode>.Failure(
            DataStoreLoaderLog.LeafFieldHasNoChild(NullLogger.Instance, Name, name));

    IDatumType IPropertyDefinition.Role => _field.Role;

    bool IPropertyDefinition.IsRequired => _field.IsRequired;

    IReadOnlyDictionary<string, object>? IPropertyDefinition.Metadata => _field.Metadata;

    IFieldType IField.FieldType => _field.FieldType;

    string? IField.TypeSystemId => _field.TypeSystemId;

    int? IField.ConverterTypeId => _field.ConverterTypeId;

    bool IField.IsIdentity => _field.IsIdentity;

    bool IField.IsComputed => _field.IsComputed;

    bool IField.IsSystemProvided => _field.IsSystemProvided;

    bool IField.HasDefault => _field.HasDefault;
}
