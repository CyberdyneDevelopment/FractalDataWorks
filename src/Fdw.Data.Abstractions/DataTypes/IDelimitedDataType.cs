namespace Fdw.Data.Abstractions;

/// <summary>
/// The view of a data type that a delimited file's vocabulary uses: everything arrives as text, so the
/// declared type is a parsing instruction rather than a storage decision.
/// </summary>
/// <remarks>
/// <para>
/// Why a delimited file needs its own vocabulary at all: it is the one format that is not self-describing.
/// A JSON document states that a value is a number; a CSV states nothing, so the type has to be declared
/// and <see cref="Format"/> has to say how to read it — which date layout, which decimal separator.
/// </para>
/// <para>
/// Nothing about storage is reachable here — no unicode flag, no precision, no binary — because a
/// delimited file has no storage semantics to expose. <see cref="IGenericDataType.AbstractType"/> is what
/// the text becomes once parsed, and that is the whole contract.
/// </para>
/// </remarks>
public interface IDelimitedDataType : IGenericDataType
{
    /// <summary>Gets the parse format for the text (e.g. a date layout, a numeric style).</summary>
    string? Format { get; }

    /// <summary>Gets the largest length accepted, or null when the type is unbounded.</summary>
    int? MaxLength { get; }
}
