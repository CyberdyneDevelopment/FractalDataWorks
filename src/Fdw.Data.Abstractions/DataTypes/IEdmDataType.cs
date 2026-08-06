namespace Fdw.Data.Abstractions;

/// <summary>
/// The view of a data type that OData's EDM vocabulary uses: named primitives that may carry a length or a
/// precision/scale facet.
/// </summary>
/// <remarks>
/// EDM sits between the other two vocabularies — like SQL Server it names the type (<c>Edm.String</c>,
/// <c>Edm.Decimal</c>) and carries length and precision facets, but like JSON Schema it has no notion of
/// unicode-versus-bytes or fixed-versus-variable length, so those stay unreachable here.
/// </remarks>
public interface IEdmDataType : IGenericDataType
{
    /// <summary>Gets the largest length the type accepts, or null when the length facet does not apply.</summary>
    int? MaxLength { get; }

    /// <summary>Gets the largest precision the type accepts, or null when the precision facet does not apply.</summary>
    int? MaxPrecision { get; }

    /// <summary>Gets the largest scale the type accepts, or null when the scale facet does not apply.</summary>
    int? MaxScale { get; }

    /// <summary>Gets a value indicating whether the type holds bytes with no text interpretation.</summary>
    bool IsBinary { get; }

    /// <summary>Gets the literal token to emit (e.g. "Edm.String").</summary>
    string NativeName { get; }
}
