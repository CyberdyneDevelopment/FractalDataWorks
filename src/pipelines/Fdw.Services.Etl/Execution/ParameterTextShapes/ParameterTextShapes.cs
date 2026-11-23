using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// The shapes a run parameter's value can have and how each is written as text: a text, a flag, the two points in time, anything
/// else that formats itself, and every other value as JSON.
/// </summary>
/// <remarks>The shapes exclude one another; a value no shape matches is written by <see cref="Of"/>'s answer, the JSON shape.</remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ParameterTextShapeBase), typeof(IParameterTextShape), typeof(ParameterTextShapes))]
public abstract partial class ParameterTextShapes : TypeCollectionBase<ParameterTextShapeBase, IParameterTextShape>
{
    /// <summary>Finds the shape of a value.</summary>
    /// <param name="value">The parameter's value.</param>
    /// <returns>The shape that matches the value, or the JSON shape when none does.</returns>
    public static IParameterTextShape Of(object value)
        => All().FirstOrDefault(shape => shape.Matches(value)) is { } matched ? matched : Json;
}
