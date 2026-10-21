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
    private readonly bool _hasDefault;
    private readonly SimpleFieldType _fieldType;

    private readonly IDatumType _datumType;

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

    // Why the stored type rather than one derived from IsSystemProvided or IsIdentity: an identity or
    // computed column is not automatically a column a query must withhold, and deciding that here would
    // change what every existing query returns without anyone asking. The container declaration
    // (DatumTypeId) says what the column is, and the builder that reads it passes it in (FDW-988).
    IDatumType IPropertyDefinition.Role => _datumType;
    bool IPropertyDefinition.IsRequired => !IsNullable;
    IReadOnlyDictionary<string, object>? IPropertyDefinition.Metadata => null;

    IFieldType IField.FieldType => _fieldType;
    string? IField.TypeSystemId => "MsSql";
    int? IField.ConverterTypeId => MsSqlConverters.BySourceType(NativeType.NativeName).Id;
    bool IField.IsIdentity => _isIdentity;
    bool IField.IsComputed => _isComputed;
    bool IField.IsSystemProvided => _isSystemProvided || _isIdentity || _isComputed;
    bool IField.HasDefault => _hasDefault;

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
        IDatumType datumType,
        bool isIdentity = false,
        bool isComputed = false,
        bool isSystemProvided = false,
        bool hasDefault = false)
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
        _datumType = datumType;
        _isIdentity = isIdentity;
        _isComputed = isComputed;
        _isSystemProvided = isSystemProvided;
        _hasDefault = hasDefault;
        _fieldType = new SimpleFieldType
        {
            TypeName = nativeType.NativeName,
            ClrType = MsSqlConverters.BySourceType(nativeType.NativeName).TargetClrType,
        };
    }
}
