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

    /// <summary>Gets the field name.</summary>
    public string Name { get; }

    /// <summary>Gets the optional human-readable description.</summary>
    public string? Description { get; }

    /// <summary>Gets the explicitly declared abstract type. Always <see langword="null"/> here — a
    /// transport with no native-type system has nothing to declare beyond the schema's type name.</summary>
    public IDataType? ExplicitType { get; }

    /// <summary>Gets the binding to a source node. Always <see langword="null"/> — a field built by
    /// this generic path is never query-bound.</summary>
    public IFieldBinding? Binding => null;

    /// <summary>Gets the zero-based ordinal position of this field within its parent container.</summary>
    public int Ordinal { get; }

    /// <summary>Gets whether this field accepts <see langword="null"/> values.</summary>
    public bool IsNullable { get; }

    /// <summary>Gets the child nodes of this field. Always empty — a field is a leaf.</summary>
    public IReadOnlyList<IDataNode> Nodes => [];

    /// <summary>A field has no children, so this always fails.</summary>
    /// <param name="name">The child name that was looked up.</param>
    /// <returns>A failure result naming this field and the requested child.</returns>
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
