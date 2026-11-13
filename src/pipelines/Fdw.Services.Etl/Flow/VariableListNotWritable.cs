using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// The signal that a list held in a variable cannot be written as quoted text (FDW-5347): it is empty, or an item is null or holds a double quote.
/// </summary>
/// <remarks>
/// Why an <see cref="InvalidOperationException"/> that carries the list's name, and no exception type of its own: <see cref="Graphs.Building.VariableText.Render"/>
/// and the value functions built on it return a value, not a result, and callers of them already expect an <see cref="InvalidOperationException"/> for a name
/// that is not in scope, so no caller changes. It never leaves a step: each step that renders a parameter text (Execute SQL, Execute pipeline, a calculation)
/// catches it where it renders, with <see cref="TryRead"/>, and returns the coded failure <c>FlowDataOptionsLog.ListNotWritable</c> (FLOW-21010).
/// </remarks>
internal static class VariableListNotWritable
{
    private const string ListNameKey = "ListName";

    /// <summary>Makes the exception for a list that cannot be written.</summary>
    /// <param name="listName">The name of the list variable.</param>
    /// <returns>The exception to throw, carrying the list's name.</returns>
    internal static InvalidOperationException Of(string listName)
    {
        var exception = new InvalidOperationException(
            $"The text names the list '{listName}', and it is empty, or an item of it is null or holds a double quote, which a quoted list cannot hold.");
        exception.Data[ListNameKey] = listName;
        return exception;
    }

    /// <summary>Says whether an exception is the signal of a list that cannot be written, and which list.</summary>
    /// <param name="exception">The exception a render threw.</param>
    /// <param name="listName">The name of the list variable when it is.</param>
    /// <returns><see langword="true"/> when the exception came from <see cref="Of"/>.</returns>
    internal static bool TryRead(InvalidOperationException exception, [NotNullWhen(true)] out string? listName)
    {
        listName = exception.Data[ListNameKey] as string;
        return listName is not null;
    }
}
