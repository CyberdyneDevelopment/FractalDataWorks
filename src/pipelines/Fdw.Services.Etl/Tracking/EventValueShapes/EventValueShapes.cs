using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Tracking;

/// <summary>
/// The shapes a value in a run event's data can have: a long, an int, a string, a bool, a moment, and the JSON numbers, booleans and
/// strings an event read back from storage holds.
/// </summary>
/// <remarks>A value is read by the shape that matches it; a value with no shape, or a shape that does not read as the figure asked for, reads as nothing: nothing is guessed.</remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(EventValueShapeBase), typeof(IEventValueShape), typeof(EventValueShapes))]
public abstract partial class EventValueShapes : TypeCollectionBase<EventValueShapeBase, IEventValueShape>
{
    /// <summary>Finds the shape of a value.</summary>
    /// <param name="value">The value in the event's data; none has no shape.</param>
    /// <returns>The shape, or <c>null</c> when the value is none or has no shape.</returns>
    public static IEventValueShape? Of(object? value)
        => value is null ? null : All().FirstOrDefault(shape => shape.Matches(value));
}
