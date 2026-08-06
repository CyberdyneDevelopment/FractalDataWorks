using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// Single-line text input component type.
/// </summary>
// Why: pure TypeOption leaf — literal constructor values only, no logic to test.
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ComponentTypes), "TextInput", RestrictToCurrentCompilation = true)]
public sealed class TextInputComponentType : ComponentTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextInputComponentType"/> class.
    /// </summary>
    public TextInputComponentType() : base(1, "TextInput", "Text Input", "Input", "Single-line text input") { }
}