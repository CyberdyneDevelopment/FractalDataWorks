using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// Multi-line text area component type.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ComponentTypes), "TextArea", RestrictToCurrentCompilation = true)]
public sealed class TextAreaComponentType : ComponentTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextAreaComponentType"/> class.
    /// </summary>
    public TextAreaComponentType() : base(3, "TextArea", "Text Area", "Input", "Multi-line text input") { }
}