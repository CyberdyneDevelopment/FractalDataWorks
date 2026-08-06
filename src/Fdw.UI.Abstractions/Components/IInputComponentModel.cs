using System;

namespace Fdw.UI.Abstractions.Components;

/// <summary>
/// Non-generic input component base for common operations.
/// </summary>
public interface IInputComponentModel : IComponentModel
{
    /// <summary>
    /// Gets or sets the current value as an object.
    /// </summary>
    object? ValueAsObject { get; set; }

    /// <summary>
    /// Gets the default value as an object.
    /// </summary>
    object? DefaultValueAsObject { get; }

    /// <summary>
    /// Gets the type of value this component holds.
    /// </summary>
    Type ValueType { get; }
}

/// <summary>
/// Input component with strongly-typed value binding.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
/// <remarks>
/// For value types (int, bool, DateTime, etc.), implementations typically use
/// <c>Nullable&lt;T&gt;</c> (e.g., <c>int?</c>) for the internal storage while
/// this interface exposes the underlying type.
/// </remarks>
public interface IInputComponentModel<T> : IInputComponentModel
{
}