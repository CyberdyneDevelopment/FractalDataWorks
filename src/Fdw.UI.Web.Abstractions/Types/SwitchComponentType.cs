using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// Boolean toggle switch component type.
/// </summary>
// Why: pure TypeOption leaf — literal constructor values only, no logic to test.
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ComponentTypes), "Switch", RestrictToCurrentCompilation = true)]
public sealed class SwitchComponentType : ComponentTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SwitchComponentType"/> class.
    /// </summary>
    public SwitchComponentType() : base(4, "Switch", "Switch", "Input", "Boolean toggle switch") { }
}