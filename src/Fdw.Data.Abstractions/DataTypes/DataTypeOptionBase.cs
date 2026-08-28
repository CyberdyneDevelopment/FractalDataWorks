using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// The one class every data type option is, in every vocabulary. Carries the union of every property any
/// vocabulary needs; each vocabulary's collection closes on the narrow interface that exposes its subset.
/// </summary>
/// <remarks>
/// <para>
/// Why a single wide class rather than a base per vocabulary with bespoke members: a type's properties are
/// constants of the option, so they belong in the constructor call, not in an <c>override</c>. This is the
/// same shape <see cref="FormatTypeBase"/> already uses. The alternative — each option declaring its own
/// <c>MaxLength</c> or <c>Precision</c> — puts the vocabulary's knowledge in N places and makes the set of
/// properties unenumerable, which is exactly what left <c>varchar(50)</c> inexpressible: the 24 SQL Server
/// options declared nothing beyond an abstract-type override, so no length could be carried at all.
/// </para>
/// <para>
/// A type simply does not set what does not apply to it. <c>bit</c> passes no length and no precision;
/// <c>varchar</c> passes a length and no precision; <c>decimal</c> passes precision and scale and no length.
/// The narrow interface for each vocabulary then hides what that vocabulary can never use.
/// </para>
/// <para>
/// Codepage, encoding and character set are deliberately absent — they are a later concern, and the
/// constructor is the one place they can be added without touching a single option that does not want them.
/// </para>
/// </remarks>
public abstract class DataTypeOptionBase
    : TypeOptionBase<int, DataTypeOptionBase>,
      IGenericDataType,
      IMsSqlDataType,
      IPostgreSqlDataType,
      IJsonSchemaDataType,
      IEdmDataType,
      IDelimitedDataType
{
    /// <summary>Initializes a new instance of the <see cref="DataTypeOptionBase"/> class.</summary>
    /// <param name="id">Identifier within the owning collection.</param>
    /// <param name="name">The type's name in its own vocabulary (e.g. "varchar", "Edm.String").</param>
    /// <param name="description">What this type holds.</param>
    /// <param name="displayName">The user-facing name, or null to use <paramref name="name"/>.</param>
    /// <param name="abstractType">The portable abstract type this one normalizes to.</param>
    /// <param name="isNumeric">Whether the type holds a number.</param>
    /// <param name="isTemporal">Whether the type holds a date, a time, or both.</param>
    /// <param name="maxLength">Largest length the type accepts, or null when length does not apply.</param>
    /// <param name="maxPrecision">Largest precision the type accepts, or null when precision does not apply.</param>
    /// <param name="maxScale">Largest scale the type accepts, or null when scale does not apply.</param>
    /// <param name="defaultLength">Length applied when a field declares none.</param>
    /// <param name="defaultPrecision">Precision applied when a field declares none.</param>
    /// <param name="defaultScale">Scale applied when a field declares none.</param>
    /// <param name="requiresLength">Whether a field of this type is meaningless without an explicit length.</param>
    /// <param name="requiresPrecision">Whether a field of this type is meaningless without an explicit precision.</param>
    /// <param name="isUnicode">Whether the type stores characters rather than bytes — decides what length counts.</param>
    /// <param name="isVariableLength">Whether the type is variable-length (varchar) rather than padded (char).</param>
    /// <param name="isBinary">Whether the type holds bytes with no text interpretation.</param>
    /// <param name="supportsStreaming">Whether values can be read without materializing the whole value.</param>
    /// <param name="isDeprecated">Whether the backend has superseded this type.</param>
    /// <param name="nativeName">The literal token to emit, when it differs from <paramref name="name"/>.</param>
    /// <param name="format">The wire format qualifier, where the vocabulary has one (JSON Schema "date-time").</param>
    protected DataTypeOptionBase(
        int id,
        string name,
        string description,
        IDataType abstractType,
        string? displayName = null,
        bool isNumeric = false,
        bool isTemporal = false,
        int? maxLength = null,
        int? maxPrecision = null,
        int? maxScale = null,
        int? defaultLength = null,
        int? defaultPrecision = null,
        int? defaultScale = null,
        bool requiresLength = false,
        bool requiresPrecision = false,
        bool isUnicode = false,
        bool isVariableLength = false,
        bool isBinary = false,
        bool supportsStreaming = false,
        bool isDeprecated = false,
        string? nativeName = null,
        string? format = null)
        : base(id, name, $"TypeOptions:{name}", displayName ?? name, description, category: null)
    {
        AbstractType = abstractType;
        IsNumeric = isNumeric;
        IsTemporal = isTemporal;
        MaxLength = maxLength;
        MaxPrecision = maxPrecision;
        MaxScale = maxScale;
        DefaultLength = defaultLength;
        DefaultPrecision = defaultPrecision;
        DefaultScale = defaultScale;
        RequiresLength = requiresLength;
        RequiresPrecision = requiresPrecision;
        IsUnicode = isUnicode;
        IsVariableLength = isVariableLength;
        IsBinary = isBinary;
        SupportsStreaming = supportsStreaming;
        IsDeprecated = isDeprecated;
        NativeName = nativeName ?? name;
        Format = format;
    }

    /// <summary>Initializes the collection's NotFound sentinel.</summary>
    /// <remarks>
    /// Why this exists: TypeCollectionGenerator builds an Empty/NotFound sentinel for every collection and
    /// cannot default a reference-typed constructor parameter (TC009). The sentinel is a real "no such
    /// type" value, so its abstract type is <c>DataTypes.NotFound</c> — the framework's own sentinel —
    /// rather than null or a fabricated stand-in. Callers compare against NotFound; they never read
    /// through it.
    /// </remarks>
    protected DataTypeOptionBase()
        : base(0, "NotFound")
    {
        AbstractType = DataTypes.NotFound;
        NativeName = "NotFound";
    }

    /// <inheritdoc />
    public IDataType AbstractType { get; }

    /// <inheritdoc />
    public bool IsNumeric { get; }

    /// <inheritdoc />
    public bool IsTemporal { get; }

    /// <inheritdoc />
    public int? MaxLength { get; }

    /// <inheritdoc />
    public int? MaxPrecision { get; }

    /// <inheritdoc />
    public int? MaxScale { get; }

    /// <inheritdoc />
    public int? DefaultLength { get; }

    /// <inheritdoc />
    public int? DefaultPrecision { get; }

    /// <inheritdoc />
    public int? DefaultScale { get; }

    /// <inheritdoc />
    public bool RequiresLength { get; }

    /// <inheritdoc />
    public bool RequiresPrecision { get; }

    /// <inheritdoc />
    public bool IsUnicode { get; }

    /// <inheritdoc />
    public bool IsVariableLength { get; }

    /// <inheritdoc />
    public bool IsBinary { get; }

    /// <inheritdoc />
    public bool SupportsStreaming { get; }

    /// <inheritdoc />
    public bool IsDeprecated { get; }

    /// <inheritdoc />
    public string NativeName { get; }

    /// <inheritdoc />
    public string? Format { get; }
}
