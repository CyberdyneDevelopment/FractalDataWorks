using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// JSON structure editor component type.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ComponentTypes), "JsonEditor", RestrictToCurrentCompilation = true)]
public sealed class JsonEditorComponentType : ComponentTypeBase
{
    /// <summary>
    /// Gets the singleton instance of the JSON editor component type.
    /// </summary>
    public JsonEditorComponentType() : base(15, "JsonEditor", "JSON Editor", "Complex", "Edit JSON data") { }
}