using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.UI.Components.RazorConsole;

/// <summary>
/// Marks a property as a component parameter.
/// Parameters can be set by parent components.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
[ExcludeFromCodeCoverage]
public sealed class ParameterAttribute : Attribute
{
}
