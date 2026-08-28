using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// Create new instance render mode.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RenderModes), "Create", RestrictToCurrentCompilation = true)]
public sealed class CreateWebRenderMode : WebRenderModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateWebRenderMode"/> class.
    /// </summary>
    public CreateWebRenderMode() : base(3, "Create", "Create Mode", "Create new items") { }
}