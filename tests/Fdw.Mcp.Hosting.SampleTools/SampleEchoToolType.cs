using System;
using Fdw.Collections.Attributes;

namespace Fdw.Mcp.Hosting.SampleTools;

/// <summary>
/// The declaration a tool package ships. No code calls this constructor — the module initializer
/// emitted in the consuming entry-point scans referenced assemblies and registers it into
/// <see cref="McpToolTypes"/> at load, which is what "reference the package and the tool appears"
/// means in practice.
/// </summary>
[TypeOption(typeof(McpToolTypes), "SampleEcho")]
public sealed class SampleEchoToolType : McpToolTypeBase
{
    /// <summary>Initializes the sample tool declaration.</summary>
    public SampleEchoToolType() : base(1, "SampleEcho") { }

    /// <inheritdoc />
    public override Type ToolClass => typeof(SampleEchoTool);
}
