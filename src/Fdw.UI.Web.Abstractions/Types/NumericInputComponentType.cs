using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// Numeric value input component type.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ComponentTypes), "NumericInput", RestrictToCurrentCompilation = true)]
public sealed class NumericInputComponentType : ComponentTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NumericInputComponentType"/> class.
    /// </summary>
    public NumericInputComponentType() : base(2, "NumericInput", "Numeric Input", "Input", "Numeric value input") { }
}