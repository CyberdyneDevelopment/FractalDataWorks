using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Logging;
using Fdw.Results;
using Fdw.Schema;
using Fdw.Schema.Properties;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// PostgreSQL-specific runtime implementation of <see cref="IPostgreSqlDataField"/> and <see cref="IField"/>.
/// Constructed by <c>DataContainerBuilder</c> and <c>ConfigurationGatewayDataStoreProvider.Load</c>.
/// </summary>
public sealed class PostgreSqlDataField : IPostgreSqlDataField, IField
{
    private readonly bool _isIdentity;
    private readonly bool _isComputed;
    private readonly SimpleFieldType _fieldType;

    // Why: PropertyRoles is populated by module initializers before any field is read by a translator.
    private static readonly IPropertyRole AttributeRole = PropertyRoles.ByName("Attribute");

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string? Description { get; }

    /// <inheritdoc />
    public IDataType? ExplicitType => NativeType == PostgreSqlNativeTypes.NotFound
        ? null
        : NativeType.AbstractType;

    /// <inheritdoc />
    public IFieldBinding? Binding => null;

    /// <inheritdoc />
    public int Ordinal { get; }

    /// <inheritdoc />
    public bool IsNullable { get; }

    /// <inheritdoc />
    // Why: a field is a leaf IDataNode — it has no children.
    public IReadOnlyList<IDataNode> Nodes => [];

    /// <inheritdoc />
    // Why: a leaf field never has child nodes, so Node(name) always fails (no Try*, no nullable).
    public IGenericResult<IDataNode> Node(string name) =>
        GenericResult<IDataNode>.Failure(
            DataNodeTreeLog.LeafFieldHasNoChild(NullLogger.Instance, Name, name));

    /// <inheritdoc />
    public PostgreSqlNativeTypeBase NativeType { get; }

    /// <inheritdoc />
    public int? Precision { get; }

    /// <inheritdoc />
    public int? Scale { get; }

    /// <inheritdoc />
    public int? MaxLength { get; }

    // -------------------------------------------------------
    // IField / IPropertyDefinition implementation
    // -------------------------------------------------------

    // Why: Role, IsRequired, Metadata are declared on IPropertyDefinition (base of IField).
    // Explicit impl uses the declaring interface name, not IField.
    IPropertyRole IPropertyDefinition.Role => AttributeRole;
    bool IPropertyDefinition.IsRequired => !IsNullable;
    IReadOnlyDictionary<string, object>? IPropertyDefinition.Metadata => null;

    IFieldType IField.FieldType => _fieldType;
    string? IField.TypeSystemId => "PostgreSql";
    int? IField.ConverterTypeId => PostgreSqlConverters.BySourceType(NativeType.Name).Id;
    bool IField.IsIdentity => _isIdentity;
    bool IField.IsComputed => _isComputed;
    bool IField.IsSystemProvided => _isIdentity || _isComputed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlDataField"/> class.
    /// </summary>
    public PostgreSqlDataField(
        string name,
        string? description,
        int ordinal,
        bool isNullable,
        PostgreSqlNativeTypeBase nativeType,
        int? precision,
        int? scale,
        int? maxLength,
        bool isIdentity = false,
        bool isComputed = false)
    {
        Name = name;
        Description = description;
        Ordinal = ordinal;
        IsNullable = isNullable;
        NativeType = nativeType;
        Precision = precision;
        Scale = scale;
        MaxLength = maxLength;
        _isIdentity = isIdentity;
        _isComputed = isComputed;
        _fieldType = new SimpleFieldType
        {
            TypeName = nativeType.Name,
            ClrType = PostgreSqlConverters.BySourceType(nativeType.Name).TargetClrType,
        };
    }
}
