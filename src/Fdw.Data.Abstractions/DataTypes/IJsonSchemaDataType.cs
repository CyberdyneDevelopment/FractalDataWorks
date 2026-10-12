namespace Fdw.Data.Abstractions;

/// <summary>
/// The view of a data type that JSON Schema's vocabulary uses: a small set of primitives, narrowed by a
/// format qualifier.
/// </summary>
/// <remarks>
/// Why format carries the weight here: JSON Schema has seven primitives, so the distinction between a
/// date-time, a uuid and an ordinary string lives entirely in <see cref="Format"/> — the opposite of SQL
/// Server, where the distinction is the type name and there is no format at all. <c>MaxLength</c> is
/// reachable because JSON Schema declares <c>maxLength</c>; precision and scale are not, because it has
/// no equivalent.
/// </remarks>
public interface IJsonSchemaDataType : IGenericDataType
{
    /// <summary>Gets the format qualifier that narrows the primitive (e.g. "date-time", "uuid", "int64").</summary>
    string? Format { get; }

    /// <summary>Gets the largest length the type accepts, or null when length does not apply.</summary>
    int? MaxLength { get; }

    /// <summary>Gets a value indicating whether the type holds bytes with no text interpretation.</summary>
    bool IsBinary { get; }
}
