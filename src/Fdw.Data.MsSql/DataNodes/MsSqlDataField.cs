using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Logging;
using Fdw.Results;
using Fdw.Schema;
using Fdw.Schema.Properties;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server-specific runtime implementation of <see cref="IMsSqlDataField"/> and <see cref="IField"/>.
/// Constructed by <c>DataContainerBuilder</c> and <c>ConfigurationGatewayDataStoreProvider.Load</c>.
/// </summary>
public sealed class MsSqlDataField : IMsSqlDataField, IField
{
    private readonly bool _isIdentity;
    private readonly bool _isComputed;
    private readonly bool _isSystemProvided;
    private readonly SimpleFieldType _fieldType;

    private static readonly IPropertyRole AttributeRole = PropertyRoles.ByName("Attribute");

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string? Description { get; }

    /// <inheritdoc />
    public IDataType? ExplicitType => NativeType == MsSqlNativeTypes.NotFound
        ? null
        : NativeType.AbstractType;

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
            DataNodeTreeLog.LeafFieldHasNoChild(NullLogger.Instance, Name, name));

    /// <inheritdoc />
    public DataTypeOptionBase NativeType { get; }

    /// <inheritdoc />
    public int? Precision { get; }

    /// <inheritdoc />
    public int? Scale { get; }

    /// <inheritdoc />
    public int? MaxLength { get; }

    /// <inheritdoc />
    public string? Collation { get; }

    // -------------------------------------------------------
    // IField / IPropertyDefinition implementation
    // -------------------------------------------------------

    IPropertyRole IPropertyDefinition.Role => AttributeRole;
    bool IPropertyDefinition.IsRequired => !IsNullable;
    IReadOnlyDictionary<string, object>? IPropertyDefinition.Metadata => null;

    IFieldType IField.FieldType => _fieldType;
    string? IField.TypeSystemId => "MsSql";
    int? IField.ConverterTypeId => MsSqlConverters.BySourceType(NativeType.Name).Id;
    bool IField.IsIdentity => _isIdentity;
    bool IField.IsComputed => _isComputed;
    bool IField.IsSystemProvided => _isSystemProvided || _isIdentity || _isComputed;

    /// <inheritdoc />
    /// <remarks>
    /// Why Visible here rather than derived from IsSystemProvided: an identity or computed column is
    /// not automatically a column a dataset must not see, and deciding that here would change what
    /// every existing query returns without anyone asking for it. The value belongs to the container
    /// declaration (VisibilityId) and is supplied by the builder that reads it.
    /// </remarks>
    IFieldVisibility IField.Visibility => FieldVisibilities.ByName("Visible");

    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlDataField"/> class.
    /// </summary>
    public MsSqlDataField(
        string name,
        string? description,
        int ordinal,
        bool isNullable,
        DataTypeOptionBase nativeType,
        int? precision,
        int? scale,
        int? maxLength,
        string? collation,
        bool isIdentity = false,
        bool isComputed = false,
        bool isSystemProvided = false)
    {
        Name = name;
        Description = description;
        Ordinal = ordinal;
        IsNullable = isNullable;
        NativeType = nativeType;
        Precision = precision;
        Scale = scale;
        MaxLength = maxLength;
        Collation = collation;
        _isIdentity = isIdentity;
        _isComputed = isComputed;
        _isSystemProvided = isSystemProvided;
        _fieldType = new SimpleFieldType
        {
            TypeName = nativeType.Name,
            ClrType = MsSqlConverters.BySourceType(nativeType.Name).TargetClrType,
        };
    }
}
