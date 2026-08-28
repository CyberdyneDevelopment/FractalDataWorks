using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Fdw.Services.Calculations.Abstractions.Lineage;

/// <summary>
/// The recorded form of a value a calculation step consumed or produced.
/// </summary>
/// <remarks>
/// <para>
/// A trace is only worth keeping if it can be persisted and re-read later, which means the values
/// in it need one agreed representation rather than a raw <see cref="object"/> every consumer
/// renders its own way. Two consumers formatting the same decimal differently — one culture-aware,
/// one not — produce two different accounts of the same execution, which is precisely the
/// ambiguity a trace exists to remove.
/// </para>
/// <para>
/// The rendering is invariant-culture and deterministic, so the same execution always yields
/// byte-identical text. It is a record of what the operation saw, not a re-hydratable value: the
/// trace is evidence of the path taken, and the calculation itself is what produces values.
/// </para>
/// </remarks>
public sealed class CalculationTraceValue
{
    /// <summary>
    /// Gets the CLR type of the value as it stood at execution time.
    /// Empty only when the value was <see langword="null"/>, which has no type to record.
    /// </summary>
    public string RuntimeType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the invariant-culture rendering of the value.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> means no text was recorded, which happens in exactly two cases,
    /// told apart by <see cref="RuntimeType"/>: the value itself was null (empty
    /// <see cref="RuntimeType"/>), or its type rendered nothing (populated
    /// <see cref="RuntimeType"/>). Neither is stood in for with a placeholder — an absent value is
    /// recorded as absent rather than as text that was never there.
    /// </remarks>
    public string? Text { get; init; }

    /// <summary>
    /// Records a runtime value in its traceable form.
    /// </summary>
    /// <param name="value">The value as the executor bound or received it.</param>
    /// <returns>The recorded form of <paramref name="value"/>.</returns>
    public static CalculationTraceValue From(object? value)
    {
        if (value is null)
        {
            return new CalculationTraceValue();
        }

        return new CalculationTraceValue
        {
            RuntimeType = value.GetType().Name,
            Text = Render(value),
        };
    }

    /// <summary>
    /// Renders a non-null value to deterministic invariant-culture text.
    /// </summary>
    private static string? Render(object value)
    {
        if (value is string text)
        {
            return text;
        }

        if (value is IFormattable formattable)
        {
            return formattable.ToString(null, CultureInfo.InvariantCulture);
        }

        if (value is IReadOnlyDictionary<string, object?> row)
        {
            return RenderRow(row);
        }

        if (value is IDictionary<string, object> mutableRow)
        {
            var copy = new Dictionary<string, object?>(mutableRow.Count, StringComparer.Ordinal);
            foreach (var pair in mutableRow)
            {
                copy[pair.Key] = pair.Value;
            }

            return RenderRow(copy);
        }

        if (value is IEnumerable sequence)
        {
            return RenderSequence(sequence);
        }

        return value.ToString();
    }

    /// <summary>
    /// Renders a row as its fields, ordered by name so the same row always renders identically.
    /// </summary>
    private static string RenderRow(IReadOnlyDictionary<string, object?> row)
    {
        var names = new List<string>(row.Keys);
        names.Sort(StringComparer.Ordinal);

        var builder = new StringBuilder("{");
        for (var index = 0; index < names.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            builder.Append(names[index]).Append('=').Append(RenderElement(row[names[index]]));
        }

        return builder.Append('}').ToString();
    }

    /// <summary>
    /// Renders a sequence in iteration order, which is the order the operation saw it in.
    /// </summary>
    private static string RenderSequence(IEnumerable sequence)
    {
        var builder = new StringBuilder("[");
        var first = true;
        foreach (var element in sequence)
        {
            if (!first)
            {
                builder.Append(", ");
            }

            builder.Append(RenderElement(element));
            first = false;
        }

        return builder.Append(']').ToString();
    }

    /// <summary>
    /// Renders one element of a row or sequence, marking absence and unrenderable types explicitly.
    /// </summary>
    /// <remarks>
    /// Why markers rather than blanks: inside a composite rendering there is nowhere to hang the
    /// separate absence flag <see cref="Text"/> provides at the top level, and eliding the element
    /// or writing empty text would silently change the shape of the row or sequence being recorded.
    /// A null element is written as <c>null</c>; an element whose type renders no text is written
    /// as its type name in angle brackets, so a reader can see something was there and what it was.
    /// </remarks>
    private static string RenderElement(object? element)
    {
        if (element is null)
        {
            return "null";
        }

        var rendered = Render(element);
        return rendered is null ? $"<{element.GetType().Name}>" : rendered;
    }
}
