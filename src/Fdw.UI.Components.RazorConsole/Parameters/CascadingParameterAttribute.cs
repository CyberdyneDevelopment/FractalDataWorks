using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.UI.Components.RazorConsole;

/// <summary>
/// Marks a property as a cascading parameter.
/// Cascading parameters flow down the component tree.
/// </summary>
// Why: pure attribute definition (declarative metadata only) — no logic to unit test.
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
[ExcludeFromCodeCoverage]
public sealed class CascadingParameterAttribute : Attribute
{
    /// <summary>
    /// Optional name for this cascading parameter.
    /// </summary>
    public string? Name { get; set; }
}
