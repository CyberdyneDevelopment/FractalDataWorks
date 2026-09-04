using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Schema;
using Fdw.Schema.Properties;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.DataNodes;

/// <summary>
/// Generic runtime implementation of <see cref="IDataField"/> and <see cref="IField"/> — a leaf
/// <see cref="IDataNode"/> for transports with no native-type system of their own.
/// </summary>
/// <remarks>
/// Why public: shared across transport-specific builders in OTHER assemblies (e.g.
/// <c>Fdw.Services.Connections.FileSystem</c>'s <c>FileSystemDataStoreBuilder</c>, mirroring
/// <c>Fdw.Services.Connections.MsSql</c>'s own <c>MsSqlDataField</c>) — genuinely generic, not a detail
/// private to this assembly.
/// </remarks>
public sealed class DataField : IDataField, IField
{
    private static readonly IPropertyRole AttributeRole = PropertyRoles.ByName("Attribute");
    private readonly SimpleFieldType _fieldType;

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string? Description { get; }

    /// <inheritdoc />
    public IDataType? ExplicitType { get; }

    /// <inheritdoc />
    public IFieldBinding? Binding => null;

    /// <inheritdoc />
    public int Ordinal { get; }

    /// <inheritdoc />
    public bool IsNullable { get; }

    /// <inheritdoc />
    public IReadOnlyList<IDataNode> Nodes => [];

    /// <inheritdoc />
    public IGenericResult<IDataNode> Node(string name) =>
        GenericResult<IDataNode>.Failure(
            DataStoreLoaderLog.LeafFieldHasNoChild(NullLogger.Instance, Name, name));

    // -------------------------------------------------------
    // IField / IPropertyDefinition implementation. No native-type system here (that's what
    // distinguishes a transport-specific field like MsSqlDataField) -- TypeSystemId and
    // ConverterTypeId stay null, which RowMappingContext already treats as "no converter,
    // read the raw value" rather than a failure.
    // -------------------------------------------------------

    IPropertyRole IPropertyDefinition.Role => AttributeRole;
    bool IPropertyDefinition.IsRequired => !IsNullable;
    IReadOnlyDictionary<string, object>? IPropertyDefinition.Metadata => null;

    IFieldType IField.FieldType => _fieldType;
    string? IField.TypeSystemId => null;
    int? IField.ConverterTypeId => null;
    bool IField.IsIdentity => false;
    bool IField.IsComputed => false;
    bool IField.IsSystemProvided => false;
    IFieldVisibility IField.Visibility => FieldVisibilities.ByName("Visible");

    /// <summary>
    /// Initializes a new instance of the <see cref="DataField"/> class.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <param name="description">Optional human-readable description.</param>
    /// <param name="explicitType">The explicitly declared abstract type, if any; <see langword="null"/> for generic transports with no native-type system.</param>
    /// <param name="ordinal">The field's declared ordinal position.</param>
    /// <param name="isNullable">Whether the field is declared nullable.</param>
    /// <param name="dataTypeName">The schema-declared type name (e.g. "int", "varchar"), carried into <see cref="IField.FieldType"/> for schema projection even though no CLR type is resolved from it.</param>
    public DataField(string name, string? description, IDataType? explicitType, int ordinal, bool isNullable, string? dataTypeName = null)
    {
        Name = name;
        Description = description;
        ExplicitType = explicitType;
        Ordinal = ordinal;
        IsNullable = isNullable;
        _fieldType = new SimpleFieldType { TypeName = dataTypeName ?? "Unknown", ClrType = typeof(object) };
    }
}
