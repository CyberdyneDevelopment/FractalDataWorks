using System;
using Fdw.Collections;
using Fdw.Data.Abstractions.Results;
using Fdw.Results;

namespace Fdw.Data.Abstractions;

/// <summary>
/// CRTP base class for abstract data type options in the <see cref="DataTypes"/> TypeCollection.
/// </summary>
/// <remarks>
/// <para>
/// Each concrete subclass represents one portable abstract type (e.g., <c>Int64</c>, <c>String</c>).
/// Storage-specific layers map their native types (e.g., SQL Server <c>bigint</c>,
/// PostgreSQL <c>int8</c>) to the appropriate <see cref="DataTypeBase"/> instance.
/// </para>
/// <para>
/// A data type also converts a value to its own CLR type, so a caller holding text (a trigger's run parameter, a stored
/// constant) never branches on the type's name: it asks the type. An option states only how to read a value
/// (<see cref="Parse"/>) and which CLR type that gives (<see cref="ClrType"/>); <see cref="Convert"/> turns a value that
/// does not read into a failure result. Members that a generated NotFound sentinel must reproduce take no <c>out</c> or
/// <c>ref</c> parameters and no delegates.
/// </para>
/// </remarks>
public abstract class DataTypeBase : TypeOptionBase<DataTypeBase>, IDataType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataTypeBase"/> class.
    /// </summary>
    /// <param name="name">Canonical name of the abstract type (e.g., "Int64", "String").</param>
    protected DataTypeBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract Type ClrType { get; }

    /// <inheritdoc />
    /// <remarks>Read off <see cref="ClrType"/>, so an option states its CLR type once.</remarks>
    public string FullName => ClrType.ToString();

    /// <inheritdoc />
    public virtual bool RendersInExpressions => true;

    /// <summary>
    /// Reads text as this type's <see cref="ClrType"/>, with the invariant culture.
    /// </summary>
    /// <param name="text">The text to read.</param>
    /// <returns>The value as <see cref="ClrType"/>, or <see langword="null"/> when the text is not a value of this type.</returns>
    /// <remarks>
    /// Why text in and an object out, and no <c>out</c> parameter or value-typed input: the NotFound sentinel the collection
    /// generates must reproduce every abstract member, and it stubs a member whose parameter type is its return type as the
    /// identity (it would hand the input back as a "converted" value). Text in and object out cannot be mistaken for that,
    /// so the sentinel's null is a refusal.
    /// </remarks>
    public abstract object? Parse(string text);

    /// <inheritdoc />
    public IGenericResult<object> Convert(object? value)
    {
        var read = value switch
        {
            null => null,
            string text => Parse(text),
            _ => value.GetType() == ClrType ? value : null,
        };
        return read is null
            ? GenericResult<object>.Failure(
                DataStoresResultCodes.ByName("DataValueNotConvertible"),
                ResultDetails.Create("DataType", Name, "ValueType", value is null ? "null" : value.GetType().Name))
            : GenericResult<object>.Success(read);
    }

    /// <inheritdoc />
    public virtual string Format(object value) => DataValueReader.InvariantText(value);
}
