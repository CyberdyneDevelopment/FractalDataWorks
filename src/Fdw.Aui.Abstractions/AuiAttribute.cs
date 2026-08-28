using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Aui;

/// <summary>
/// Marks a UI component or action as accessible via the Agent User Interface (AUI).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
[ExcludeFromCodeCoverage]
public sealed class AuiAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the human-readable description of the agent-accessible element.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tool name for this action.
    /// </summary>
    public string? ToolName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this tool requires explicit human confirmation.
    /// </summary>
    public bool RequiresConfirmation { get; set; }
}
