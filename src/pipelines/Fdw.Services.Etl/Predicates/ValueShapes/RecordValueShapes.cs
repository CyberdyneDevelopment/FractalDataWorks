using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Predicates;

/// <summary>
/// The shapes a value in a record can have that a condition reads in its own way: a bool, a string, the two points in time and the
/// numeric types; every other value is <c>Plain</c>.
/// </summary>
/// <remarks>A value is read by the shape that matches it; a value no shape matches is read as <see cref="Of"/> answers, the plain shape.</remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(RecordValueShapeBase), typeof(IRecordValueShape), typeof(RecordValueShapes))]
public abstract partial class RecordValueShapes : TypeCollectionBase<RecordValueShapeBase, IRecordValueShape>
{
    /// <summary>Finds the shape of a value.</summary>
    /// <param name="value">A value in the record that is not none.</param>
    /// <returns>The shape that matches the value, or the plain shape when none does.</returns>
    public static IRecordValueShape Of(object value)
        => All().FirstOrDefault(shape => shape.Matches(value)) is { } matched ? matched : Plain;
}
