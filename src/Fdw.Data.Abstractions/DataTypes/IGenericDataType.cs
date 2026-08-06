using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// The properties every data type carries, whatever vocabulary it belongs to — SQL Server native types,
/// PostgreSQL native types, JSON Schema types, OData EDM types, or a delimited file's declared types.
/// </summary>
/// <remarks>
/// <para>
/// Why one narrow root and per-vocabulary extensions: <see cref="DataTypeOptionBase"/> is a single class
/// carrying the union of every property any vocabulary needs, and each vocabulary's collection closes on
/// the interface that exposes only the subset meaningful to it. So <c>MsSqlNativeTypes.ByName("varchar")</c>
/// hands back a view on which <c>MaxLength</c> is reachable and <c>Format</c> is not, without a bespoke
/// class per type or a per-vocabulary base. The narrowing is structural, not documented.
/// </para>
/// <para>
/// These four are the properties that mean the same thing in every vocabulary. Anything whose meaning is
/// vocabulary-specific — a length limit, a precision limit, a wire format — belongs on the derived
/// interface for that vocabulary, not here.
/// </para>
/// </remarks>
public interface IGenericDataType : ITypeOption<int, DataTypeOptionBase>
{
    /// <summary>Gets the type's name in its own vocabulary (e.g. "varchar", "Edm.String", "integer").</summary>
    // Why Description is not redeclared here: TypeOptionBase already carries Id, Name, DisplayName,
    // Description, Category and ConfigurationKey for every option in the framework.
    new string Name { get; }

    /// <summary>Gets the portable abstract type this one normalizes to (e.g. SQL Server <c>bigint</c> → Int64).</summary>
    /// <remarks>
    /// Why every vocabulary carries this: it is what makes a DataSet portable across backends. A field
    /// discovered as <c>nvarchar</c> and a field discovered as JSON Schema <c>string</c> are the same
    /// abstract String, which is the only reason a pipeline can read one and write the other.
    /// </remarks>
    IDataType AbstractType { get; }

    /// <summary>Gets a value indicating whether this type holds a number.</summary>
    bool IsNumeric { get; }

    /// <summary>Gets a value indicating whether this type holds a date, a time, or both.</summary>
    bool IsTemporal { get; }
}
